using SchoolAdmission.Application.Abstractions;
using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Domain.Entities;
using SchoolAdmission.Domain.Exceptions;

namespace SchoolAdmission.Application.Services;

public sealed class ClassService(
    IAdmissionRepository repository,
    IIdGenerator ids,
    TimeProvider clock) : IClassService
{
    public async Task<ClassResponse> CreateAsync(CreateClassRequest? request, CancellationToken ct)
    {
        if (request is null)
            throw new ValidationException("Request body is required.");

        // SchoolClass.Create validates name and seat count.
        var schoolClass = SchoolClass.Create(
            ids.NewClassId(),
            request.Name ?? string.Empty,
            request.TotalSeats,
            clock.GetUtcNow().UtcDateTime);

        await repository.CreateClassAsync(schoolClass, ct);

        return new ClassResponse(schoolClass.Id, schoolClass.Name, schoolClass.TotalSeats, 0, 0);
    }

    public Task<IReadOnlyList<ClassResponse>> ListAsync(CancellationToken ct) =>
        repository.ListClassesAsync(ct);

    public Task<ClassResponse> GetAsync(string classId, CancellationToken ct) =>
        repository.GetClassAsync(classId, ct);
}