using SchoolAdmission.Application.Abstractions;

namespace SchoolAdmission.Infrastructure.Services;

public sealed class GuidIdGenerator : IIdGenerator
{
    public string NewClassId() => $"c_{Guid.NewGuid():N}";
    public string NewHoldId() => $"h_{Guid.NewGuid():N}";
}