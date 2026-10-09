using VetRIS.Domain.Common;

namespace VetRIS.Domain.Entities.Admin;

/// <summary>Consolidates the spec's separate "Configurations" and "Settings" tables into one key/value store.</summary>
public class SystemConfiguration : BaseEntity
{
    public string Key { get; private set; } = default!;
    public string Value { get; private set; } = default!;
    public string Category { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsEditable { get; private set; } = true;

    private SystemConfiguration()
    {
    } // EF Core

    public SystemConfiguration(string key, string value, string category, string? description = null, bool isEditable = true)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Configuration key is required.", nameof(key));

        Key = key;
        Value = value;
        Category = category;
        Description = description;
        IsEditable = isEditable;
    }

    public void UpdateValue(string value)
    {
        if (!IsEditable) throw new InvalidOperationException($"Configuration '{Key}' is not editable.");
        Value = value;
    }
}
