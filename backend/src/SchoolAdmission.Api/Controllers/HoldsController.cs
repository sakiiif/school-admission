using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Application.Services;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("holds")]
public class HoldsController(IHoldService holds) : ControllerBase
{
    // 5. GET /holds/{holdId}
    [HttpGet("{holdId}")]
    public async Task<ActionResult<HoldResponse>> Get(string holdId, CancellationToken ct) =>
        Ok(await holds.GetAsync(holdId, ct));

    // 6. POST /holds/{holdId}/confirm
    [HttpPost("{holdId}/confirm")]
    public async Task<ActionResult<ConfirmResponse>> Confirm(
        string holdId,
        [FromHeader(Name = "X-Student-Id")] string? studentId,
        CancellationToken ct) =>
        Ok(await holds.ConfirmAsync(holdId, studentId, ct));

    // 7. DELETE /holds/{holdId}
    [HttpDelete("{holdId}")]
    public async Task<IActionResult> Cancel(
        string holdId,
        [FromHeader(Name = "X-Student-Id")] string? studentId,
        CancellationToken ct)
    {
        await holds.CancelAsync(holdId, studentId, ct);
        return NoContent(); // 204
    }
}