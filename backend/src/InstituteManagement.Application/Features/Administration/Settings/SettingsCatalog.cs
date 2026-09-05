namespace InstituteManagement.Application.Features.Administration.Settings;

public static partial class SettingsCatalog
{
    private static readonly IReadOnlyList<SettingsSectionDefinition> CatalogSections =
    [
        InstituteSection,
        AcademicYearSection,
        SemesterSection,
        DepartmentsSection,
        CoursesSection,
        ClassroomsSection,
        UsersAccessSection,
        StudentRulesSection,
        TeacherRulesSection,
        AttendanceRulesSection,
        GradeRulesSection,
        NotificationsSection,
        SystemSection,
        SecuritySection
    ];

    private static readonly IReadOnlyDictionary<string, SettingsSectionDefinition> CatalogByName =
        CatalogSections.ToDictionary(section => section.Name, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<SettingsSectionDefinition> Sections => CatalogSections;

    public static SettingsSectionDefinition GetSection(string section) =>
        !string.IsNullOrWhiteSpace(section) && CatalogByName.TryGetValue(section, out var definition)
            ? definition
            : throw new KeyNotFoundException("Settings section not found.");

    public static Dictionary<string, string> Defaults(string section) =>
        GetSection(section).Settings.ToDictionary(
            setting => setting.Key,
            setting => setting.DefaultValue,
            StringComparer.Ordinal);
}
