namespace InstituteManagement.Application.Features.Administration.Settings;

public static partial class SettingsCatalog
{
    private static readonly SettingsSectionDefinition DepartmentsSection = Section("departments",
        Option("defaultStatus", "Active", "Active", "Inactive"),
        Boolean("requireDepartmentHead", true),
        Boolean("allowCrossDepartmentTeaching", false));

    private static readonly SettingsSectionDefinition CoursesSection = Section("courses",
        Integer("defaultCapacity", "40", 1, 10000),
        Boolean("requireAssignedTeacher", true));

    private static readonly SettingsSectionDefinition ClassroomsSection = Section("classrooms",
        Integer("defaultCapacity", "40", 1, 10000),
        Boolean("attendanceDeviceRequired", true));
}
