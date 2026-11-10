namespace SchoolAdmission.Application.DTOs;

public sealed record ClassResponse(
    string Id,
    string Name,
    int TotalSeats,
    int HeldSeats,
    int ConfirmedSeats)
{
    // Derived, never stored.
    public int AvailableSeats => Math.Max(0, TotalSeats - HeldSeats - ConfirmedSeats);
}