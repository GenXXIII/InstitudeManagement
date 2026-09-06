namespace InstituteManagement.Application.Features.Administration.Settings;

public static partial class SettingsCatalog
{
    private static readonly SettingsSectionDefinition CodeFormatsSection = new(
        "code-formats",
        [
            Boolean("codeIncludeYear", false),
            Digits("codeStartingNumber", "1", 12),
            Integer("codePaddingWidth", "1", 1, 12),
            Option("codeSeparator", "-", "-", "/", ".", "_"),
            ..ResourceCodeFormat("student", "STU", "ESTU"),
            ..ResourceCodeFormat("teacher", "TEA", "ETEA"),
            ..ResourceCodeFormat("department", "DEP", "EDEP"),
            ..ResourceCodeFormat("course", "COU", "ECOU"),
            ..ResourceCodeFormat("classroom", "CLA", "ECLA"),
            ..ResourceCodeFormat("timetable", "TIM", "ETIM"),
            ..ResourceCodeFormat("attendance", "ATT", "EATT"),
            ..ResourceCodeFormat("grade", "GRD", "EGRD"),
            ..ResourceCodeFormat("session", "SES", "ESES"),
            ..ResourceCodeFormat("alert", "ALT", "EALT")
        ]);

    private static SettingDefinition[] ResourceCodeFormat(string resource, string management, string enrollment) =>
        [
            Code($"{resource}ManagementPrefix", management, 16),
            Code($"{resource}EnrollmentPrefix", enrollment, 16),
            Code($"{resource}OperationPrefix", "OPE", 16),
            Code($"{resource}RecordPrefix", "REC", 16),
            Code($"{resource}HistoryPrefix", "HIS", 16)
        ];
}
