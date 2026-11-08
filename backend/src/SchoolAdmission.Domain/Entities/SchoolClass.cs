using System;
using System.Collections.Generic;
using System.Text;

using SchoolAdmission.Domain.Exceptions;

namespace SchoolAdmission.Domain.Entities;

public class SchoolClass
{
    public const int MaxNameLength = 200;

    public string Id { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public int TotalSeats { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private SchoolClass() { } // EF Core

    public static SchoolClass Create(string id, string name, int totalSeats, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ValidationException("Class id is required.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("Class name is required.");

        name = name.Trim();
        if (name.Length > MaxNameLength)
            throw new ValidationException($"Class name must be at most {MaxNameLength} characters.");

        if (totalSeats <= 0)
            throw new ValidationException("Total seats must be greater than zero.");

        return new SchoolClass
        {
            Id = id,
            Name = name,
            TotalSeats = totalSeats,
            CreatedAt = nowUtc
        };
    }
}
