using SchoolAdmission.Domain.Entities;
using SchoolAdmission.Domain.Enums;

namespace SchoolAdmission.Application.Abstractions;

// A hold + what the outside world should see: the class name (for /me/holds)
// and the effective status, computed with the DATABASE clock. A Held row past
// its expiry is reported as Expired even before the background job updates it.
public sealed record HoldView(Hold Hold, string ClassName, HoldStatus EffectiveStatus);