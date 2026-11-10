namespace SchoolAdmission.Application.Abstractions;

public interface IIdGenerator
{
    string NewClassId();  // e.g. "c_3f9a1b2c..."
    string NewHoldId();   // e.g. "h_7d21e0aa..."
}