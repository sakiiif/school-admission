using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Domain.Entities;

namespace SchoolAdmission.Application.Abstractions;

// Implemented in Infrastructure. Every method that changes seat state must be atomic
// and use the database clock (never the app clock) for expiry decisions
public interface IAdmissionRepository
{
    Task CreateClassAsync(SchoolClass schoolClass, CancellationToken ct);

    // Classes with held/confirmed counts, computed from the holds table
    Task<IReadOnlyList<ClassResponse>> ListClassesAsync(CancellationToken ct);

    /// <exception cref="Domain.Exceptions.ClassNotFoundException"/>
    Task<ClassResponse> GetClassAsync(string classId, CancellationToken ct);

    // Locks the class row, checks duplicate + capacity, inserts the hold, all in one transaction.
    /// <exception cref="Domain.Exceptions.ClassNotFoundException"/>
    /// <exception cref="Domain.Exceptions.DuplicateActiveHoldException"/>
    /// <exception cref="Domain.Exceptions.NoSeatsAvailableException"/>
    Task<HoldView> ReserveSeatAsync(string holdId, string classId, string studentId,
                                    TimeSpan duration, CancellationToken ct);

    /// <exception cref="Domain.Exceptions.HoldNotFoundException"/>
    Task<HoldView> GetHoldAsync(string holdId, CancellationToken ct);

    // Idempotent. Owner only
    /// <exception cref="Domain.Exceptions.HoldNotFoundException"/>
    /// <exception cref="Domain.Exceptions.NotHoldOwnerException"/>
    /// <exception cref="Domain.Exceptions.HoldExpiredException"/>
    /// <exception cref="Domain.Exceptions.InvalidHoldStateException"/>
    Task<HoldView> ConfirmHoldAsync(string holdId, string studentId, CancellationToken ct);

    /// <exception cref="Domain.Exceptions.HoldNotFoundException"/>
    /// <exception cref="Domain.Exceptions.NotHoldOwnerException"/>
    /// <exception cref="Domain.Exceptions.InvalidHoldStateException"/>
    Task CancelHoldAsync(string holdId, string studentId, CancellationToken ct);

    // Newest first
    Task<IReadOnlyList<HoldView>> ListStudentHoldsAsync(string studentId, CancellationToken ct);

    // Marks Held rows past expiry as Expired. Returns rows changed
    Task<int> ExpireDueHoldsAsync(CancellationToken ct);
}