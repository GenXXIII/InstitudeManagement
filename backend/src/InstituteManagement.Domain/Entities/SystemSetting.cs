namespace InstituteManagement.Domain.Entities;

public sealed class SystemSetting : Entity
{
    public string SystemSettingCode { get; set; } = $"SET-{Guid.NewGuid():N}";
    public required string Section { get; set; }
    public required string Key { get; set; }
    public string Value { get; set; } = string.Empty;
}
