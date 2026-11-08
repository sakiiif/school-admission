using System;
using System.Collections.Generic;
using System.Text;

using System.Text.RegularExpressions;
using SchoolAdmission.Domain.Enums;
using SchoolAdmission.Domain.Exceptions;

namespace SchoolAdmission.Domain.Entities;

public class Hold
{
    private static readonly Regex StudentIdPattern = new(@"^\d{17}$", RegexOptions.Compiled);

    public string Id { get; private set; } = default!;
    public string ClassId { get; private set; } = default!;
    public string StudentId { get; private set; } = default!;
    public HoldStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }

    private Hold() { } // EF Core

    public static bool IsValidStudentId(string? studentId) =>
        studentId is not null && StudentIdPattern.IsMatch(studentId);

    public static Hold Create(string id, string classId, string studentId,
                              DateTime nowUtc, TimeSpan duration)
    {
        if (!IsValidStudentId(studentId))
            throw new ValidationException("Student registration number must be exactly 17 digits.");

        if (duration <= TimeSpan.Zero)
            throw new ValidationException("Hold duration must be positive.");

        return new Hold
        {
            Id = id,
            ClassId = classId,
            StudentId = studentId,
            Status = HoldStatus.Held,
            CreatedAt = nowUtc,
            ExpiresAt = nowUtc + duration
        };
    }

    //True if the stored status is Held but the time has already passed.
    public bool IsExpiredAt(DateTime nowUtc) =>
        Status == HoldStatus.Held && ExpiresAt <= nowUtc;

    // Status as the outside world should see it. A Held row past its expiry is reported
    // as Expired even if the background job has not updated the row yet.
    public HoldStatus EffectiveStatus(DateTime nowUtc) =>
        IsExpiredAt(nowUtc) ? HoldStatus.Expired : Status;

    // Does this hold currently occupy a seat?
    public bool OccupiesSeatAt(DateTime nowUtc) =>
        Status == HoldStatus.Confirmed || (Status == HoldStatus.Held && ExpiresAt > nowUtc);

    // Idempotent: confirming an already confirmed hold is a no-op.
    // Does NOT mutate on expiry; it only throws. The caller may persist MarkExpired separately.
    public void Confirm(string requesterStudentId, DateTime nowUtc)
    {
        EnsureOwner(requesterStudentId);

        switch (Status)
        {
            case HoldStatus.Confirmed:
                return;

            case HoldStatus.Cancelled:
                throw new InvalidHoldStateException("This application was cancelled and cannot be confirmed.");

            case HoldStatus.Expired:
                throw new HoldExpiredException();

            case HoldStatus.Held:
                if (ExpiresAt <= nowUtc)
                    throw new HoldExpiredException();

                Status = HoldStatus.Confirmed;
                ConfirmedAt = nowUtc;
                return;
        }
    }

    // Idempotent for Cancelled and Expired. A confirmed admission cannot be cancelled
    // (refunds are out of scope).
    public void Cancel(string requesterStudentId, DateTime nowUtc)
    {
        EnsureOwner(requesterStudentId);

        switch (Status)
        {
            case HoldStatus.Cancelled:
            case HoldStatus.Expired:
                return;

            case HoldStatus.Confirmed:
                throw new InvalidHoldStateException("A confirmed admission cannot be cancelled.");

            case HoldStatus.Held:
                Status = ExpiresAt <= nowUtc ? HoldStatus.Expired : HoldStatus.Cancelled;
                return;
        }
    }

    // Used by the repository / background job to tidy stale Held rows.
    public void MarkExpired(DateTime nowUtc)
    {
        if (IsExpiredAt(nowUtc))
            Status = HoldStatus.Expired;
    }

    private void EnsureOwner(string requesterStudentId)
    {
        if (!string.Equals(StudentId, requesterStudentId, StringComparison.Ordinal))
            throw new NotHoldOwnerException();
    }
}
