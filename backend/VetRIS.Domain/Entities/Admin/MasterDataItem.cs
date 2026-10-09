using VetRIS.Domain.Common;

namespace VetRIS.Domain.Entities.Admin;

/// <summary>Admin-configurable lookup values (body parts, referral sources, GST slabs, etc.).</summary>
public class MasterDataItem : BaseEntity
{
    public string Category { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public string DisplayValue { get; private set; } = default!;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    private MasterDataItem()
    {
    } // EF Core

    public MasterDataItem(string category, string code, string displayValue, int sortOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException("Category is required.", nameof(category));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required.", nameof(code));

        Category = category;
        Code = code;
        DisplayValue = displayValue;
        SortOrder = sortOrder;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
