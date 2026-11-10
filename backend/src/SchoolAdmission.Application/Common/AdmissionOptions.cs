namespace SchoolAdmission.Application.Common;

public sealed class AdmissionOptions
{
    public const int DefaultHoldDurationSeconds = 600;

    public int HoldDurationSeconds { get; init; } = DefaultHoldDurationSeconds;

    public TimeSpan HoldDuration => TimeSpan.FromSeconds(HoldDurationSeconds);
}