using Microsoft.EntityFrameworkCore;
using Npgsql;
using SchoolAdmission.Application.Abstractions;
using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Domain.Entities;
using SchoolAdmission.Domain.Enums;
using SchoolAdmission.Domain.Exceptions;

namespace SchoolAdmission.Infrastructure.Persistence.Repositories;

public sealed class AdmissionRepository(AppDbContext db) : IAdmissionRepository
{
    // ─── Classes ───

    public async Task CreateClassAsync(SchoolClass schoolClass, CancellationToken ct)
    {
        db.Classes.Add(schoolClass);
        await db.SaveChangesAsync(ct);
    }

    public Task<IReadOnlyList<ClassResponse>> ListClassesAsync(CancellationToken ct) =>
        LoadClassesAsync(classId: null, ct);

    public async Task<ClassResponse> GetClassAsync(string classId, CancellationToken ct)
    {
        var rows = await LoadClassesAsync(classId, ct);
        return rows.Count == 0 ? throw new ClassNotFoundException(classId) : rows[0];
    }

    // helper for ListClassesAsync and GetClassAsync
    private async Task<IReadOnlyList<ClassResponse>> LoadClassesAsync(string? classId, CancellationToken ct)
    {
        var now = await GetDbNowAsync(ct);

        var query = db.Classes.AsNoTracking();
        if (classId is not null)
            query = query.Where(c => c.Id == classId);

        // Counts are computed from the holds table every time, so they can never drift.
        var rows = await query
            .OrderBy(c => c.CreatedAt).ThenBy(c => c.Id)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.TotalSeats,
                Held = db.Holds.Count(h => h.ClassId == c.Id && h.Status == HoldStatus.Held && h.ExpiresAt > now),
                Confirmed = db.Holds.Count(h => h.ClassId == c.Id && h.Status == HoldStatus.Confirmed)
            })
            .ToListAsync(ct);

        return rows
            .Select(r => new ClassResponse(r.Id, r.Name, r.TotalSeats, r.Held, r.Confirmed))
            .ToList();
    }

    // ──── Apply(hold a seat) ────

    public async Task<HoldView> ReserveSeatAsync(
        string holdId, string classId, string studentId, TimeSpan duration, CancellationToken ct)
    {
        // READ COMMITTED (default) is enough: the class-row lock does the serializing.
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // 1. Lock the class row. Every apply/confirm/cancel for this class now queues here,
        //  even across multiple backend processes, because the lock lives in Postgres.
        var cls = await LockClassAsync(classId, ct);

        // 2. Read the DB clock AFTER the lock is held, so the time is fresh.
        var now = await GetDbNowAsync(ct);

        // 3. Expire this student's own stale holds in this class, otherwise the partial
        // unique index would block a legitimate re-apply after expiry.
        await db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""Holds""
               SET ""Status"" = 'Expired'
             WHERE ""ClassId"" = {classId}
               AND ""StudentId"" = {studentId}
               AND ""Status"" = 'Held'
               AND ""ExpiresAt"" <= {now}", ct);

        // 4. Duplicate check, for a clear 409 before the index would fire.
        var alreadyActive = await db.Holds.AnyAsync(h =>
            h.ClassId == classId
            && h.StudentId == studentId
            && (h.Status == HoldStatus.Confirmed || (h.Status == HoldStatus.Held && h.ExpiresAt > now)), ct);
        if (alreadyActive)
            throw new DuplicateActiveHoldException();

        // 5. Capacity check: confirmed + unexpired held seats.
        var occupied = await db.Holds.CountAsync(h => h.ClassId == classId && (h.Status == HoldStatus.Confirmed
                || (h.Status == HoldStatus.Held && h.ExpiresAt > now)), ct);
        if (occupied >= cls.TotalSeats)
            throw new NoSeatsAvailableException();

        // 6. Insert the hold. ExpiresAt = DB now + duration.
        var hold = Hold.Create(holdId, classId, studentId, now, duration);
        db.Holds.Add(hold);

        try
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Backup: the partial unique index caught a duplicate we somehow missed.
            throw new DuplicateActiveHoldException();
        }

        return new HoldView(hold, cls.Name, HoldStatus.Held);
    }

    // ── Read a hold ──

    public async Task<HoldView> GetHoldAsync(string holdId, CancellationToken ct)
    {
        var now = await GetDbNowAsync(ct);

        var row = await (
                from h in db.Holds.AsNoTracking()
                join c in db.Classes on h.ClassId equals c.Id
                where h.Id == holdId
                select new { Hold = h, ClassName = c.Name })
            .SingleOrDefaultAsync(ct)
            ?? throw new HoldNotFoundException(holdId);

        return new HoldView(row.Hold, row.ClassName, row.Hold.EffectiveStatus(now));
    }

    public async Task<IReadOnlyList<HoldView>> ListStudentHoldsAsync(string studentId, CancellationToken ct)
    {
        var now = await GetDbNowAsync(ct);

        var rows = await (
                from h in db.Holds.AsNoTracking()
                join c in db.Classes on h.ClassId equals c.Id
                where h.StudentId == studentId
                orderby h.CreatedAt descending
                select new { Hold = h, ClassName = c.Name })
            .ToListAsync(ct);

        return rows
            .Select(r => new HoldView(r.Hold, r.ClassName, r.Hold.EffectiveStatus(now)))
            .ToList();
    }

    // ── Confirm(pay admission fee) ──

    public async Task<HoldView> ConfirmHoldAsync(string holdId, string studentId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Same class lock as ReserveSeat: confirm and reserve can never interleave,
        // so a seat that expired and was given away cannot be "confirmed" afterwards.
        var (hold, cls) = await LockHoldAndClassAsync(holdId, ct);
        var now = await GetDbNowAsync(ct);

        // Owner check, idempotency and expiry rules live in the domain.
        // If it throws, the transaction rolls back; the expiry job tidies the status.
        hold.Confirm(studentId, now);

        await db.SaveChangesAsync(ct);   // no-op if it was already Confirmed
        await tx.CommitAsync(ct);

        return new HoldView(hold, cls.Name, hold.EffectiveStatus(now));
    }

    // ── Cancel ──

    public async Task CancelHoldAsync(string holdId, string studentId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Locked too, so cancel cannot race with confirm on the same hold.
        var (hold, _) = await LockHoldAndClassAsync(holdId, ct);
        var now = await GetDbNowAsync(ct);

        hold.Cancel(studentId, now);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    // ──── Expiry job support ────

    public async Task<int> ExpireDueHoldsAsync(CancellationToken ct)
    {
        // Idempotent and safe to run from several processes at once.
        // It never changes the seat count: expired holds were already not counted.
        return await db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""Holds""
               SET ""Status"" = 'Expired'
             WHERE ""Status"" = 'Held'
               AND ""ExpiresAt"" <= clock_timestamp()", ct);
    }

    // ─── Helpers ───

    // SELECT ... FOR UPDATE on the class row. Throws if the class does not exist
    private async Task<SchoolClass> LockClassAsync(string classId, CancellationToken ct)
    {
        var rows = await db.Classes
            .FromSqlInterpolated($"SELECT * FROM \"Classes\" WHERE \"Id\" = {classId} FOR UPDATE")
            .AsNoTracking()
            .ToListAsync(ct);

        return rows.FirstOrDefault() ?? throw new ClassNotFoundException(classId);
    }

    // Finds the hold's class, locks that class row, then re-reads the hold under the lock
    // so we act on the latest committed state.
    private async Task<(Hold Hold, SchoolClass Class)> LockHoldAndClassAsync(string holdId, CancellationToken ct)
    {
        var classId = await db.Holds
            .AsNoTracking()
            .Where(h => h.Id == holdId)
            .Select(h => h.ClassId)
            .FirstOrDefaultAsync(ct)
            ?? throw new HoldNotFoundException(holdId);

        var cls = await LockClassAsync(classId, ct);

        var hold = await db.Holds.SingleOrDefaultAsync(h => h.Id == holdId, ct)
                   ?? throw new HoldNotFoundException(holdId);

        return (hold, cls);
    }

    // Current time from the database server, so multiple backends agree
    private async Task<DateTime> GetDbNowAsync(CancellationToken ct) =>
        await db.Database
            .SqlQuery<DateTime>($"SELECT clock_timestamp() AS \"Value\"")
            .SingleAsync(ct);

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

}