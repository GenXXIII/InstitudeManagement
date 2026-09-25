using System.Text.RegularExpressions;
using InstituteManagement.Application.Features.Administration.Settings;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Services.Grades;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Persistence;

public static class SettingsCatalogSeeder
{
    private static readonly IReadOnlyDictionary<string, string> LegacyEnrollmentPrefixes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["student"] = "ESTU",
        ["teacher"] = "ETEA",
        ["department"] = "EDEP",
        ["course"] = "ECOU",
        ["classroom"] = "ECLA",
        ["timetable"] = "ETIM",
        ["attendance"] = "EATT",
        ["grade"] = "EGRD",
        ["session"] = "ESES"
    };
    private static readonly string[] LegacyNotificationCodeKeys =
    [
        "alertCodePrefix", "notificationCodePrefix", "historyCodePrefix", "codeIncludeYear",
        "codeStartingNumber", "codePaddingWidth", "codeSeparator"
    ];
    private static readonly string[] ObsoleteAlertCodeKeys =
    [
        "alertManagementPrefix", "alertEnrollmentPrefix", "alertOperationPrefix", "alertRecordPrefix", "alertHistoryPrefix"
    ];
    private static readonly string[] ObsoleteGradeKeys =
    [
        "aPlusMinimum", "bPlusMinimum", "cPlusMinimum",
        "aPlusGpa", "bPlusGpa", "cPlusGpa"
    ];
    private static readonly string[] ObsoleteFinanceProviderKeys =
    [
        "abaEnabled", "abaAccountName", "abaAccountCode",
        "acledaEnabled", "acledaAccountName", "acledaAccountCode"
    ];
    private static readonly string[] AttendanceResultKeys =
    [
        "absentScoreDeduction", "permissionScoreDeduction", "retakeAbsentSections", "failAbsentSections",
        "retakePermissionSections", "failPermissionSections"
    ];

    public static async Task SeedMissingAsync(InstituteDbContext db, CancellationToken cancellationToken = default)
    {
        await MoveNotificationCodeSettingsAsync(db, cancellationToken);
        await RemoveObsoleteAlertCodeSettingsAsync(db, cancellationToken);
        await RemoveObsoleteGradeSettingsAsync(db, cancellationToken);
        await NormalizeLegacyFinancePaymentMethodsAsync(db, cancellationToken);
        await NormalizeLegacyStudentPublicIdPrefixAsync(db, cancellationToken);
        await NormalizeLegacyEnrollmentPrefixesAsync(db, cancellationToken);
        await BackfillEnrollmentWorkflowCodesAsync(db, cancellationToken);
        var existing = await db.SystemSettings.AsNoTracking()
            .Select(setting => new { setting.Section, setting.Key })
            .ToListAsync(cancellationToken);
        if (existing.Count == 0) return;

        var existingKeys = existing
            .Select(setting => CompositeKey(setting.Section, setting.Key))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hadGradeRules = existing.Any(setting => setting.Section.Equals("grade-rules", StringComparison.OrdinalIgnoreCase));
        var now = DateTime.UtcNow;
        var missing = new List<SystemSetting>();
        var obsoleteInstituteRegionalSettings = await db.SystemSettings
            .Where(setting => setting.Section == "institute" && (setting.Key == "timeZone" || setting.Key == "dateFormat"))
            .ToListAsync(cancellationToken);
        var obsoleteFinanceProviderSettings = await db.SystemSettings
            .Where(setting => setting.Section == "finance" && ObsoleteFinanceProviderKeys.Contains(setting.Key))
            .ToListAsync(cancellationToken);

        foreach (var section in SettingsCatalog.Sections)
            foreach (var setting in section.Settings)
            {
                if (!existingKeys.Add(CompositeKey(section.Name, setting.Key))) continue;
                missing.Add(new SystemSetting
                {
                    Section = section.Name,
                    Key = setting.Key,
                    Value = setting.DefaultValue,
                    CreateAt = now,
                    UpdatedAtUtc = now
                });
            }

        if (missing.Count > 0) db.SystemSettings.AddRange(missing);
        if (obsoleteInstituteRegionalSettings.Count > 0) db.SystemSettings.RemoveRange(obsoleteInstituteRegionalSettings);
        if (obsoleteFinanceProviderSettings.Count > 0) db.SystemSettings.RemoveRange(obsoleteFinanceProviderSettings);
        if (missing.Count > 0 || obsoleteInstituteRegionalSettings.Count > 0 || obsoleteFinanceProviderSettings.Count > 0)
            await db.SaveChangesAsync(cancellationToken);

        var hasLegacyGrades = await db.GradeRecords.AsNoTracking()
            .AnyAsync(grade => grade.LetterGrade != "A" && grade.LetterGrade != "B" && grade.LetterGrade != "C" && grade.LetterGrade != "D" && grade.LetterGrade != "E" && grade.LetterGrade != "F", cancellationToken);
        var addedGradeRules = missing.Any(setting => setting.Section == "grade-rules");
        var addedAttendanceResultRules = missing.Any(setting => setting.Section == "attendance-rules" && AttendanceResultKeys.Contains(setting.Key));
        if (!hadGradeRules || addedGradeRules || addedAttendanceResultRules || hasLegacyGrades)
        {
            var storedGradeRules = await db.SystemSettings.AsNoTracking()
                .Where(setting => setting.Section == "grade-rules")
                .ToDictionaryAsync(setting => setting.Key, setting => setting.Value, cancellationToken);
            var storedAttendanceRules = await db.SystemSettings.AsNoTracking()
                .Where(setting => setting.Section == "attendance-rules")
                .ToDictionaryAsync(setting => setting.Key, setting => setting.Value, cancellationToken);
            var scale = GradeThresholds.From(storedGradeRules);
            var weights = GradeWeights.From(storedGradeRules);
            var attendanceRules = AttendanceResultRules.From(storedAttendanceRules);
            var sessions = await db.ClassSessionRecords.AsNoTracking().ToListAsync(cancellationToken);
            var sessionsByPeriodCourse = sessions.ToLookup(item => (item.AcademicYear, item.Term, item.CourseId));
            foreach (var grade in await db.GradeRecords.ToListAsync(cancellationToken))
            {
                var attendance = GradeCompositionCalculator.Attendance(sessionsByPeriodCourse[(grade.AcademicYear, grade.Term, grade.CourseId)], grade.StudentId, weights.Attendance, attendanceRules);
                GradeCompositionCalculator.Apply(grade, weights, scale, attendance, grade.AssignmentScore, grade.MidtermScore, grade.FinalExamScore);
                grade.UpdatedAtUtc = now;
            }
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task MoveNotificationCodeSettingsAsync(InstituteDbContext db, CancellationToken cancellationToken)
    {
        var legacy = await db.SystemSettings
            .Where(setting => setting.Section == "notifications" && LegacyNotificationCodeKeys.Contains(setting.Key))
            .ToListAsync(cancellationToken);
        if (legacy.Count == 0) return;

        var codeFormatKeys = await db.SystemSettings.AsNoTracking()
            .Where(setting => setting.Section == "code-formats")
            .Select(setting => setting.Key)
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var setting in legacy)
        {
            var prefixBelongsInCodeFormats = setting.Key is "alertCodePrefix" or "notificationCodePrefix" or "historyCodePrefix";
            if (prefixBelongsInCodeFormats && codeFormatKeys.Add(setting.Key)) setting.Section = "code-formats";
            else db.SystemSettings.Remove(setting);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task RemoveObsoleteAlertCodeSettingsAsync(InstituteDbContext db, CancellationToken cancellationToken)
    {
        var obsolete = await db.SystemSettings
            .Where(setting => setting.Section == "code-formats" && ObsoleteAlertCodeKeys.Contains(setting.Key))
            .ToListAsync(cancellationToken);
        if (obsolete.Count == 0) return;
        db.SystemSettings.RemoveRange(obsolete);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task RemoveObsoleteGradeSettingsAsync(InstituteDbContext db, CancellationToken cancellationToken)
    {
        var obsolete = await db.SystemSettings
            .Where(setting => setting.Section == "grade-rules" && ObsoleteGradeKeys.Contains(setting.Key))
            .ToListAsync(cancellationToken);
        if (obsolete.Count == 0) return;
        db.SystemSettings.RemoveRange(obsolete);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task NormalizeLegacyFinancePaymentMethodsAsync(InstituteDbContext db, CancellationToken cancellationToken)
    {
        var paymentMethods = await db.SystemSettings
            .SingleOrDefaultAsync(setting => setting.Section == "finance" && setting.Key == "paymentMethods", cancellationToken);
        if (paymentMethods is null) return;

        var normalized = paymentMethods.Value
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(method => !method.Equals("ABA", StringComparison.OrdinalIgnoreCase) && !method.Equals("ACLEDA", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (!normalized.Contains("Bakong", StringComparer.OrdinalIgnoreCase)) normalized.Insert(Math.Min(1, normalized.Count), "Bakong");
        var value = string.Join(',', normalized);
        if (paymentMethods.Value == value) return;

        paymentMethods.Value = value;
        paymentMethods.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task BackfillEnrollmentWorkflowCodesAsync(InstituteDbContext db, CancellationToken cancellationToken)
    {
        var format = await BusinessCodeFormatter.LoadAsync(db, cancellationToken);
        var changed = false;

        var studentEnrollments = await db.StudentEnrollments.Include(item => item.Student).ToListAsync(cancellationToken);
        var studentOccurrences = Occurrences(studentEnrollments, item => item.StudentId);
        foreach (var enrollment in studentEnrollments)
        {
            if (enrollment.Student is null) continue;
            var occurrence = studentOccurrences[enrollment.Id];
            var codes = Codes(format, enrollment.Student.StudentCode, enrollment.EnrollmentCode, "student", occurrence);
            changed |= Assign(enrollment, codes);
            if (!PublicAccessId.MatchesEnrollment(enrollment.PublicId, enrollment.Id))
            {
                enrollment.PublicId = format.EnrollmentPublicId(enrollment.Id, "studentPublicIdPrefix", "STU");
                changed = true;
            }
            if (string.IsNullOrWhiteSpace(enrollment.FinanceCode))
            {
                enrollment.FinanceCode = format.LinkedWithConfiguredPrefix(enrollment.Student.StudentCode, "student", occurrence, "financeCodePrefix", "FIN");
                changed = true;
            }
            if (string.IsNullOrWhiteSpace(enrollment.ResultCode))
            {
                enrollment.ResultCode = format.LinkedWithConfiguredPrefix(enrollment.Student.StudentCode, "student", occurrence, "resultCodePrefix", "RES");
                changed = true;
            }
        }
        var teacherAssignments = await db.TeacherAssignments.Include(item => item.Teacher).ToListAsync(cancellationToken);
        var teacherOccurrences = Occurrences(teacherAssignments, item => item.TeacherId);
        foreach (var assignment in teacherAssignments)
        {
            if (assignment.Teacher is null) continue;
            var occurrence = teacherOccurrences[assignment.Id];
            var codes = Codes(format, assignment.Teacher.TeacherCode, assignment.EnrollmentCode, "teacher", occurrence);
            changed |= Assign(assignment, codes);
            if (!PublicAccessId.MatchesEnrollment(assignment.PublicId, assignment.Id))
            {
                assignment.PublicId = format.EnrollmentPublicId(assignment.Id, "teacherPublicIdPrefix", "TEA");
                changed = true;
            }
        }
        var courseAssignments = await db.CourseAssignments.Include(item => item.Course).ToListAsync(cancellationToken);
        var courseOccurrences = Occurrences(courseAssignments, item => item.CourseId);
        foreach (var assignment in courseAssignments)
        {
            if (assignment.Course is not null) changed |= Assign(assignment, Codes(format, assignment.Course.CourseCode, assignment.EnrollmentCode, "course", courseOccurrences[assignment.Id]));
        }
        var classroomAssignments = await db.ClassroomAssignments.Include(item => item.Classroom).ToListAsync(cancellationToken);
        var classroomOccurrences = Occurrences(classroomAssignments, item => item.ClassroomId);
        foreach (var assignment in classroomAssignments)
        {
            if (assignment.Classroom is not null) changed |= Assign(assignment, Codes(format, assignment.Classroom.ClassroomCode, assignment.EnrollmentCode, "classroom", classroomOccurrences[assignment.Id]));
        }
        var timetableEnrollments = await db.TimetableEnrollments.Include(item => item.ScheduleEntry).ToListAsync(cancellationToken);
        var timetableOccurrences = Occurrences(timetableEnrollments, item => item.ScheduleEntryId);
        foreach (var enrollment in timetableEnrollments)
        {
            if (enrollment.ScheduleEntry is not null) changed |= Assign(enrollment, Codes(format, enrollment.ScheduleEntry.TimetableCode, enrollment.EnrollmentCode, "timetable", timetableOccurrences[enrollment.Id]));
        }

        if (changed) await db.SaveChangesAsync(cancellationToken);
    }

    private static Dictionary<Guid, long> Occurrences<T>(IEnumerable<T> rows, Func<T, Guid> sourceId) where T : Entity =>
        rows.GroupBy(sourceId).SelectMany(group => group.OrderBy(item => item.CreateAt).ThenBy(item => item.Id)
            .Select((item, index) => new { item.Id, Occurrence = (long)index + 1 }))
            .ToDictionary(item => item.Id, item => item.Occurrence);

    private static BusinessCodeFormatter.WorkflowCodeChain Codes(
        BusinessCodeFormatter.BusinessCodeFormat format,
        string managementCode,
        string enrollmentCode,
        string resource,
        long occurrence)
    {
        var normalizedEnrollment = IsLegacyEnrollmentCode(enrollmentCode, resource)
            ? format.Linked(managementCode, resource, "enrollment", occurrence)
            : enrollmentCode;
        return new(
            managementCode,
            normalizedEnrollment,
            format.Linked(managementCode, resource, "operation", occurrence),
            format.Linked(managementCode, resource, "record", occurrence),
            format.Linked(managementCode, resource, "history", occurrence));
    }

    private static bool Assign(StudentEnrollment item, BusinessCodeFormatter.WorkflowCodeChain codes) => AssignCodes(
        () => item.EnrollmentCode, value => item.EnrollmentCode = value,
        () => item.OperationCode, value => item.OperationCode = value,
        () => item.RecordCode, value => item.RecordCode = value,
        () => item.HistoryCode, value => item.HistoryCode = value,
        codes);

    private static bool Assign(TeacherAssignment item, BusinessCodeFormatter.WorkflowCodeChain codes) => AssignCodes(
        () => item.EnrollmentCode, value => item.EnrollmentCode = value,
        () => item.OperationCode, value => item.OperationCode = value,
        () => item.RecordCode, value => item.RecordCode = value,
        () => item.HistoryCode, value => item.HistoryCode = value,
        codes);

    private static bool Assign(CourseAssignment item, BusinessCodeFormatter.WorkflowCodeChain codes) => AssignCodes(
        () => item.EnrollmentCode, value => item.EnrollmentCode = value,
        () => item.OperationCode, value => item.OperationCode = value,
        () => item.RecordCode, value => item.RecordCode = value,
        () => item.HistoryCode, value => item.HistoryCode = value,
        codes);

    private static bool Assign(ClassroomAssignment item, BusinessCodeFormatter.WorkflowCodeChain codes) => AssignCodes(
        () => item.EnrollmentCode, value => item.EnrollmentCode = value,
        () => item.OperationCode, value => item.OperationCode = value,
        () => item.RecordCode, value => item.RecordCode = value,
        () => item.HistoryCode, value => item.HistoryCode = value,
        codes);

    private static bool Assign(TimetableEnrollment item, BusinessCodeFormatter.WorkflowCodeChain codes) => AssignCodes(
        () => item.EnrollmentCode, value => item.EnrollmentCode = value,
        () => item.OperationCode, value => item.OperationCode = value,
        () => item.RecordCode, value => item.RecordCode = value,
        () => item.HistoryCode, value => item.HistoryCode = value,
        codes);

    private static bool AssignCodes(
        Func<string> enrollment, Action<string> setEnrollment,
        Func<string> operation, Action<string> setOperation,
        Func<string> record, Action<string> setRecord,
        Func<string> history, Action<string> setHistory,
        BusinessCodeFormatter.WorkflowCodeChain codes)
    {
        var changed = false;
        if (!enrollment().Equals(codes.Enrollment, StringComparison.Ordinal)) { setEnrollment(codes.Enrollment); changed = true; }
        if (string.IsNullOrWhiteSpace(operation())) { setOperation(codes.Operation); changed = true; }
        if (string.IsNullOrWhiteSpace(record())) { setRecord(codes.Record); changed = true; }
        if (string.IsNullOrWhiteSpace(history())) { setHistory(codes.History); changed = true; }
        return changed;
    }

    private static bool IsLegacyEnrollmentCode(string code, string resource) =>
        LegacyEnrollmentPrefixes.TryGetValue(resource, out var prefix)
        && Regex.IsMatch(
            code,
            $@"[-/._]{Regex.Escape(prefix)}[-/._]\d+$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static async Task NormalizeLegacyEnrollmentPrefixesAsync(InstituteDbContext db, CancellationToken cancellationToken)
    {
        var legacyDefaults = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ESTU", "ETEA", "EDEP", "ECOU", "ECLA", "ETIM", "EATT", "EGRD", "ESES" };
        var settings = await db.SystemSettings
            .Where(item => item.Section == "code-formats" && item.Key.EndsWith("EnrollmentPrefix"))
            .ToListAsync(cancellationToken);
        var changed = false;
        foreach (var setting in settings.Where(item => legacyDefaults.Contains(item.Value)))
        {
            setting.Value = "ENR";
            setting.UpdatedAtUtc = DateTime.UtcNow;
            changed = true;
        }
        if (changed) await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task NormalizeLegacyStudentPublicIdPrefixAsync(InstituteDbContext db, CancellationToken cancellationToken)
    {
        var setting = await db.SystemSettings.FirstOrDefaultAsync(
            item => item.Section == "code-formats" && item.Key == "studentPublicIdPrefix",
            cancellationToken);
        if (setting is null || !setting.Value.Equals("SID", StringComparison.OrdinalIgnoreCase)) return;
        setting.Value = "STU";
        setting.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string CompositeKey(string section, string key) => $"{section}\u001f{key}";
}
