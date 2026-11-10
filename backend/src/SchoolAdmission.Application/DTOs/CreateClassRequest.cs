namespace SchoolAdmission.Application.DTOs;

public sealed record CreateClassRequest(string? Name, int TotalSeats);