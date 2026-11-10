using SchoolAdmission.Application.DTOs;

namespace SchoolAdmission.Application.Services;

public interface IHoldService
{
    Task<HoldResponse> ApplyAsync(string classId, string? studentId, CancellationToken ct);
    Task<HoldResponse> GetAsync(string holdId, CancellationToken ct);
    Task<ConfirmResponse> ConfirmAsync(string holdId, string? studentId, CancellationToken ct);
    Task CancelAsync(string holdId, string? studentId, CancellationToken ct);
    Task<IReadOnlyList<MyHoldResponse>> ListMineAsync(string? studentId, CancellationToken ct);
}