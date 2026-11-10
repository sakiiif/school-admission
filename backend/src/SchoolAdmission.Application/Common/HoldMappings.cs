using SchoolAdmission.Application.Abstractions;
using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Domain.Enums;

namespace SchoolAdmission.Application.Common;

public static class HoldMappings
{
    /// Domain "Held" becomes API "HELD"
    public static string ToApiString(this HoldStatus status) => status.ToString().ToUpperInvariant();

    public static HoldResponse ToHoldResponse(this HoldView view) => new(
        view.Hold.Id,
        view.Hold.ClassId,
        view.Hold.StudentId,
        view.EffectiveStatus.ToApiString(),
        view.Hold.ExpiresAt);

    public static ConfirmResponse ToConfirmResponse(this HoldView view) => new(
        view.Hold.Id,
        view.EffectiveStatus.ToApiString());

    public static MyHoldResponse ToMyHoldResponse(this HoldView view) => new(
        view.Hold.Id,
        view.Hold.ClassId,
        view.ClassName,
        view.EffectiveStatus.ToApiString(),
        view.Hold.ExpiresAt);
}