namespace InstituteManagement.Application.DTOs;

public sealed record SettingsDto(string Section, Dictionary<string, string> Values);
