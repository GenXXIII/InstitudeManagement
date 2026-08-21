namespace InstituteManagement.Domain.Entities;

public sealed class SystemSetting : Entity
{
    public required string Section { get; set; }
    public required string Key { get; set; }
    public string Value { get; set; } = string.Empty;
}
