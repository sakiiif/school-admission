using SchoolAdmission.Application.Abstractions;
using SchoolAdmission.Application.Common;
using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Domain.Entities;
using SchoolAdmission.Domain.Exceptions;

namespace SchoolAdmission.Application.Services;

public sealed class HoldService(
    IAdmissionRepository repository,
    IIdGenerator ids,
    AdmissionOptions options) : IHoldService
{
    public async Task<HoldResponse> ApplyAsync(string classId, string? studentId, CancellationToken ct)
    {
        var student = RequireStudentId(studentId);
        var view = await repository.ReserveSeatAsync(
            ids.NewHoldId(), classId, student, options.HoldDuration, ct);
        return view.ToHoldResponse();
    }

    public async Task<HoldResponse> GetAsync(string holdId, CancellationToken ct)
    {
        var view = await repository.GetHoldAsync(holdId, ct);
        return view.ToHoldResponse();
    }

    public async Task<ConfirmResponse> ConfirmAsync(string holdId, string? studentId, CancellationToken ct)
    {
        var student = RequireStudentId(studentId);
        var view = await repository.ConfirmHoldAsync(holdId, student, ct);
        return view.ToConfirmResponse();
    }

    public Task CancelAsync(string holdId, string? studentId, CancellationToken ct)
    {
        var student = RequireStudentId(studentId);
        return repository.CancelHoldAsync(holdId, student, ct);
    }

    public async Task<IReadOnlyList<MyHoldResponse>> ListMineAsync(string? studentId, CancellationToken ct)
    {
        var student = RequireStudentId(studentId);
        var views = await repository.ListStudentHoldsAsync(student, ct);
        return views.Select(v => v.ToMyHoldResponse()).ToList();
    }

    private static string RequireStudentId(string? studentId)
    {
        if (string.IsNullOrWhiteSpace(studentId))
            throw new ValidationException("The X-Student-Id header is required.");

        if (!Hold.IsValidStudentId(studentId))
            throw new ValidationException("Student registration number must be exactly 17 digits.");

        return studentId;
    }
}