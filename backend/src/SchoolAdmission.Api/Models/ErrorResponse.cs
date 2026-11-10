namespace SchoolAdmission.Api.Models;

// The single error shape returned by every failing endpoint
public sealed record ErrorResponse(string Error, string Message);