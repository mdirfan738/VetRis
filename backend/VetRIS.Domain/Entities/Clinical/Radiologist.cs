using VetRIS.Domain.Common;

namespace VetRIS.Domain.Entities.Clinical;

/// <summary>
/// Business profile for a user in the Radiologist role. The identity record
/// itself (login, password hash, MFA) lives in ASP.NET Identity, which is
/// an Infrastructure concern; this entity holds only the data the RIS
/// domain actually needs — license, specialization, signature, workload cap.
/// </summary>
public class Radiologist : BaseEntity
{
    public Guid ApplicationUserId { get; private set; }
    public string LicenseNumber { get; private set; } = default!;
    public string? Specialization { get; private set; }
    public string? SignatureImageStorageKey { get; private set; }
    public int MaxDailyWorkload { get; private set; } = 20;
    public bool IsActive { get; private set; } = true;

    private Radiologist()
    {
    } // EF Core

    public Radiologist(Guid applicationUserId, string licenseNumber, string? specialization = null, int maxDailyWorkload = 20)
    {
        if (applicationUserId == Guid.Empty) throw new ArgumentException("A linked user account is required.", nameof(applicationUserId));
        if (string.IsNullOrWhiteSpace(licenseNumber)) throw new ArgumentException("License number is required.", nameof(licenseNumber));

        ApplicationUserId = applicationUserId;
        LicenseNumber = licenseNumber;
        Specialization = specialization;
        MaxDailyWorkload = maxDailyWorkload;
    }

    public void SetSignature(string storageKey) => SignatureImageStorageKey = storageKey;
    public void UpdateWorkloadCap(int maxDailyWorkload) => MaxDailyWorkload = maxDailyWorkload;
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
