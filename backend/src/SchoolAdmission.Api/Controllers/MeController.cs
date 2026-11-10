using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Application.Services;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("me")]
public class MeController(IHoldService holds) : ControllerBase
{
    // 8. GET /me/holds
    [HttpGet("holds")]
    public async Task<ActionResult<IReadOnlyList<MyHoldResponse>>> MyHolds(
        [FromHeader(Name = "X-Student-Id")] string? studentId,
        CancellationToken ct) =>
        Ok(await holds.ListMineAsync(studentId, ct));
}