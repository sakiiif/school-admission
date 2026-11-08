using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolAdmission.Domain.Exceptions;

// 400
public sealed class ValidationException(string message) : DomainException("VALIDATION_ERROR", message);

// 404
public sealed class ClassNotFoundException(string classId) : DomainException("CLASS_NOT_FOUND", $"Class '{classId}' was not found.");

public sealed class HoldNotFoundException(string holdId) : DomainException("HOLD_NOT_FOUND", $"Application '{holdId}' was not found.");

// 403
public sealed class NotHoldOwnerException() : DomainException("NOT_HOLD_OWNER", "This application belongs to a different student.");

// 409
public sealed class NoSeatsAvailableException() : DomainException("NO_SEATS_AVAILABLE", "No seats are available in this class.");

public sealed class DuplicateActiveHoldException() : DomainException("DUPLICATE_ACTIVE_HOLD", "You already have an active application or confirmed admission in this class.");

public sealed class HoldExpiredException() : DomainException("HOLD_EXPIRED", "This application has expired. Please apply again.");

public sealed class InvalidHoldStateException(string message) : DomainException("INVALID_HOLD_STATE", message);
