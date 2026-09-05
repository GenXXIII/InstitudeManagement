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
            ..ResourceCodeFormat("student", "STU", "ESTU", "OSTU", "RSTU", "HSTU"),
            ..ResourceCodeFormat("teacher", "TEA", "ETEA", "OTEA", "RTEA", "HTEA"),
            ..ResourceCodeFormat("department", "DEP", "EDEP", "ODEP", "RDEP", "HDEP"),
            ..ResourceCodeFormat("course", "COU", "ECOU", "OCOU", "RCOU", "HCOU"),
            ..ResourceCodeFormat("classroom", "CLA", "ECLA", "OCLA", "RCLA", "HCLA"),
            ..ResourceCodeFormat("timetable", "TIM", "ETIM", "OTIM", "RTIM", "HTIM"),
            ..ResourceCodeFormat("attendance", "ATT", "EATT", "OATT", "RATT", "HATT"),
            ..ResourceCodeFormat("grade", "GRD", "EGRD", "OGRD", "RGRD", "HGRD"),
            ..ResourceCodeFormat("session", "SES", "ESES", "OSES", "RSES", "HSES")
        ]);

    private static SettingDefinition[] ResourceCodeFormat(
        string resource,
        string management,
        string enrollment,
        string operation,
        string record,
        string history) =>
        [
            Code($"{resource}ManagementPrefix", management, 16),
            Code($"{resource}EnrollmentPrefix", enrollment, 16),
            Code($"{resource}OperationPrefix", operation, 16),
            Code($"{resource}RecordPrefix", record, 16),
            Code($"{resource}HistoryPrefix", history, 16)
        ];
}
