using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolAdmission.Domain.Exceptions;

// Base type for all business-rule violations.
// Code is a stable machine-readable string; the API layer maps it to an HTTP status.
public abstract class DomainException : Exception
{
    public string Code { get; }

    protected DomainException(string code, string message) : base(message)
    {
        Code = code;
    }
}
