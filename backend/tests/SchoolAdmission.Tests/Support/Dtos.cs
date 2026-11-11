namespace SchoolAdmission.Tests.Support;

// Separate copies of the API contract on purpose, not the Application DTOs.
// The tests assert on these fields, so a renamed JSON field fails a test.
public sealed record ClassDto(string Id, string Name, int TotalSeats, int HeldSeats, int ConfirmedSeats, int AvailableSeats);
public sealed record HoldDto(string HoldId, string ClassId, string StudentId, string Status, DateTime ExpiresAt);
public sealed record ConfirmDto(string HoldId, string Status);
public sealed record MyHoldDto(string HoldId, string ClassId, string ClassName, string Status, DateTime ExpiresAt);
public sealed record ErrorDto(string Error, string Message);