namespace InstituteManagement.Application.Features.Administration.Settings;

public static partial class SettingsCatalog
{
    private static readonly SettingsSectionDefinition AcademicYearSection = Section("academic-year",
        Text("currentYear", "2026–2027", 32),
        Code("code", "AY2026", 32),
        Date("startsOn", "2026-09-01"),
        Date("endsOn", "2027-08-31"),
        Option("status", "Active", "Active", "Upcoming", "Completed", "Inactive"));

    private static readonly SettingsSectionDefinition SemesterSection = Section("semester",
        Option("currentTerm", "Semester 1", "Semester 1", "Semester 2", "Summer Term"),
        Date("startsOn", "2026-09-01"),
        Date("endsOn", "2027-01-31"),
        Text("semester1Name", "Semester 1", 64),
        Code("semester1Code", "SEM1", 32),
        Date("semester1StartsOn", "2026-09-01"),
        Date("semester1EndsOn", "2027-01-31"),
        Option("semester1Status", "Active", "Active", "Upcoming", "Completed", "Inactive"),
        Text("semester2Name", "Semester 2", 64),
        Code("semester2Code", "SEM2", 32),
        Date("semester2StartsOn", "2027-02-01"),
        Date("semester2EndsOn", "2027-06-30"),
        Option("semester2Status", "Upcoming", "Active", "Upcoming", "Completed", "Inactive"),
        Text("summerName", "Summer Term", 64),
        Code("summerCode", "SUMMER", 32),
        Date("summerStartsOn", "2027-07-01"),
        Date("summerEndsOn", "2027-08-31"),
        Option("summerStatus", "Upcoming", "Active", "Upcoming", "Completed", "Inactive"));
}
