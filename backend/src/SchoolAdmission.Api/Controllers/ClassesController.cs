using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Application.Services;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("classes")]
public class ClassesController(IClassService classes, IHoldService holds) : ControllerBase
{
    // 1. POST /classes
    [HttpPost]
    public async Task<ActionResult<ClassResponse>> Create(
        [FromBody] CreateClassRequest? request, CancellationToken ct)
    {
        var created = await classes.CreateAsync(request, ct);
        return Created($"/classes/{created.Id}", created); // 201
    }

    // 2. GET /classes
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClassResponse>>> List(CancellationToken ct) =>
        Ok(await classes.ListAsync(ct));

    // 3. GET /classes/{classId}
    [HttpGet("{classId}")]
    public async Task<ActionResult<ClassResponse>> Get(string classId, CancellationToken ct) =>
        Ok(await classes.GetAsync(classId, ct));

    // 4. POST /classes/{classId}/holds
    [HttpPost("{classId}/holds")]
    public async Task<ActionResult<HoldResponse>> Apply(
        string classId,
        [FromHeader(Name = "X-Student-Id")] string? studentId,
        CancellationToken ct)
    {
        var hold = await holds.ApplyAsync(classId, studentId, ct);
        return Created($"/holds/{hold.HoldId}", hold); // 201
    }
}