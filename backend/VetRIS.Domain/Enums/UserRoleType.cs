namespace VetRIS.Domain.Enums;

/// <summary>
/// Fixed role set from the spec. Mirrors the ASP.NET Identity roles seeded
/// in Infrastructure (AspNetRoles) — kept as an enum here so Domain and
/// Application logic can reason about roles without depending on Identity.
/// </summary>
public enum UserRoleType
{
    SuperAdmin = 1,
    HospitalAdmin = 2,
    ClinicUser = 3,
    Radiologist = 4,
    Technician = 5,
    Billing = 6,
    Accounts = 7,
    Receptionist = 8
}
