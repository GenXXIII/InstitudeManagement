using System.Globalization;
using InstituteManagement.Application.Features.Record;
using InstituteManagement.Application.Common.Pagination;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Record.OperationalRecordFields;

namespace InstituteManagement.Infrastructure.Services.Record;

public sealed class StudentOperationalRecordReader(InstituteDbContext db) : IOperationalRecordReader
{
    public string Module => "students";

    public Task<IReadOnlyList<OperationalRecordDto>> GetAsync(Guid? departmentId, CancellationToken cancellationToken) =>
        BuildAsync(departmentId, null, null, cancellationToken);

    public async Task<StudentOperationalRecordPage> GetPageAsync(Guid? departmentId, int? year, string? selectedPeriod, string? search, bool history, PageRequest page, CancellationToken cancellationToken)
    {
        var separator = selectedPeriod?.IndexOf('|') ?? -1;
        var selectedAcademicYear = separator < 0 ? null : selectedPeriod![..separator];
        var selectedTerm = separator < 0 ? null : selectedPeriod![(separator + 1)..];
        var students = db.Students.AsNoTracking().Where(student =>
            (!departmentId.HasValue || student.DepartmentId == departmentId)
            && (!year.HasValue || db.StudentEnrollments.Any(enrollment => enrollment.StudentId == student.Id && enrollment.YearLevel == year.Value))
            && (string.IsNullOrWhiteSpace(selectedAcademicYear) || db.StudentEnrollments.Any(enrollment =>
                enrollment.StudentId == student.Id
                && enrollment.AcademicYear == selectedAcademicYear
                && (string.IsNullOrWhiteSpace(selectedTerm) || enrollment.Semester == selectedTerm)))
            && (history
                ? db.AuditLogs.Any(log => log.Type == "Student" && log.Action == "Graduated" && log.ResourceId == student.Id)
                : !db.AuditLogs.Any(log => log.Type == "Student" && log.Action == "Graduated" && log.ResourceId == student.Id)));
        var term = search?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
            students = students.Where(student =>
                student.FullName.Contains(term)
                || student.StudentCode.Contains(term)
                || student.Email.Contains(term)
                || student.Department!.Name.Contains(term)
                || db.StudentEnrollments.Any(enrollment => enrollment.StudentId == student.Id && (enrollment.EnrollmentCode.Contains(term) || enrollment.PublicId.Contains(term)))
                || db.GradeRecords.Any(grade => grade.StudentId == student.Id && (grade.GradeCode.Contains(term) || grade.Course!.CourseCode.Contains(term) || grade.Course.Name.Contains(term))));

        if (history)
        {
            var totalCount = await students.CountAsync(cancellationToken);
            var ids = await students.OrderBy(student => student.FullName).ThenBy(student => student.Id)
                .Skip(page.Skip).Take(page.NormalizedPageSize).Select(student => student.Id).ToListAsync(cancellationToken);
            return new(await BuildAsync(departmentId, ids, null, cancellationToken), totalCount);
        }

        var periods = db.StudentEnrollments.AsNoTracking()
            .Where(enrollment => (!year.HasValue || enrollment.YearLevel == year.Value)
                && (string.IsNullOrWhiteSpace(selectedAcademicYear) || enrollment.AcademicYear == selectedAcademicYear)
                && (string.IsNullOrWhiteSpace(selectedTerm) || enrollment.Semester == selectedTerm))
            .Join(students, enrollment => enrollment.StudentId, student => student.Id, (enrollment, student) => new { StudentId = student.Id, student.FullName, enrollment.AcademicYear, Term = enrollment.Semester })
            .Distinct();
        var periodCount = await periods.CountAsync(cancellationToken);
        var selectedPeriods = await periods
            .OrderBy(period => period.FullName)
            .ThenByDescending(period => period.AcademicYear)
            .ThenBy(period => period.Term)
            .ThenBy(period => period.StudentId)
            .Skip(page.Skip)
            .Take(page.NormalizedPageSize)
            .ToListAsync(cancellationToken);
        var selectedIds = selectedPeriods.Select(period => period.StudentId).Distinct().ToList();
        var periodKeys = selectedPeriods.Select(period => (period.StudentId, period.AcademicYear, period.Term)).ToHashSet();
        return new(await BuildAsync(departmentId, selectedIds, periodKeys, cancellationToken), periodCount);
    }

    private async Task<IReadOnlyList<OperationalRecordDto>> BuildAsync(Guid? departmentId, IReadOnlyList<Guid>? selectedStudentIds, HashSet<(Guid StudentId, string AcademicYear, string Term)>? selectedPeriods, CancellationToken cancellationToken)
    {
        var codeFormat = await BusinessCodeFormatter.LoadAsync(db, cancellationToken);
        var students = await db.Students.AsNoTracking().Include(x => x.Department)
            .Where(x => (!departmentId.HasValue || x.DepartmentId == departmentId) && (selectedStudentIds == null || selectedStudentIds.Contains(x.Id)))
            .OrderBy(x => x.FullName).ToListAsync(cancellationToken);
        var ids = students.Select(x => x.Id).ToList();
        var enrollments = await db.StudentEnrollments.AsNoTracking().Include(x => x.Department).Where(x => ids.Contains(x.StudentId)).ToListAsync(cancellationToken);
        var sessions = await db.ClassSessionRecords.AsNoTracking()
            .Include(x => x.ScheduleEntry)
            .Include(x => x.StudentAttendance.Where(attendance => ids.Contains(attendance.StudentId)))
            .Where(x => (!departmentId.HasValue || x.DepartmentId == departmentId) && x.StudentAttendance.Any(attendance => ids.Contains(attendance.StudentId)))
            .ToListAsync(cancellationToken);
        var grades = await db.GradeRecords.AsNoTracking().Include(x => x.Course).Where(x => ids.Contains(x.StudentId)).ToListAsync(cancellationToken);
        var studentSessions = sessions
            .SelectMany(session => session.StudentAttendance.Select(student => (Session: session, Student: student)))
            .Where(x => ids.Contains(x.Student.StudentId))
            .ToList();
        return students.Select(student =>
        {
            var completed = studentSessions.Where(x => x.Student.StudentId == student.Id && (selectedPeriods == null || selectedPeriods.Contains((student.Id, x.Session.AcademicYear, x.Session.Term)))).ToList();
            var studentGrades = grades.Where(x => x.StudentId == student.Id && (selectedPeriods == null || selectedPeriods.Contains((student.Id, x.AcademicYear, x.Term)))).ToList();
            var studentEnrollments = enrollments.Where(x => x.StudentId == student.Id && (selectedPeriods == null || selectedPeriods.Contains((student.Id, x.AcademicYear, x.Semester)))).ToList();
            var enrollmentEvents = studentEnrollments.Select(x =>
            {
                var codes = codeFormat.StudentPeriodChain(student.StudentCode, x.EnrollmentCode, x.YearLevel, x.Semester);
                return (At: x.UpdatedAtUtc, Activity: Create(
                    ("Activity", "Student enrollment"),
                    ("Management code", codes.Management),
                    ("Enrollment code", codes.Enrollment),
                    ("Operation code", codes.Operation),
                    ("Record code", codes.Record),
                    ("History code", codes.History),
                    ("Permanent code", codes.Management),
                    ("Academic year", x.AcademicYear),
                    ("Term", x.Semester),
                    ("Date", x.UpdatedAtUtc.ToString("yyyy-MM-dd")),
                    ("Time", x.UpdatedAtUtc.ToString("HH:mm")),
                    ("Year", $"Year {x.YearLevel}"),
                    ("Shift", x.Shift),
                    ("Department", x.Department?.Name ?? student.Department?.Name ?? "Unassigned"),
                    ("Enrollment status", x.Status)));
            });
            var attendanceEvents = completed.Where(x => selectedPeriods == null || selectedPeriods.Contains((student.Id, x.Session.AcademicYear, x.Session.Term))).Select(x => (At: x.Session.UpdatedAtUtc, Activity: Create(
                ("Activity", "Class attendance"), ("ClassSessionId", x.Session.Id.ToString()),
                ("CourseId", x.Session.CourseId.ToString()),
                ("Class session code", SessionCode(x.Session)), ("Timetable code", x.Session.ScheduleEntry?.TimetableCode ?? "Not recorded"),
                ("Academic year", x.Session.AcademicYear), ("Term", x.Session.Term),
                ("Date", x.Session.SessionDate.ToString("yyyy-MM-dd")), ("Time", $"{x.Session.StartsAt:HH:mm} – {x.Session.EndsAt:HH:mm}"),
                ("Year", $"Year {x.Session.YearLevel}"), ("Course", x.Session.CourseName),
                ("Teacher", x.Session.TeacherName), ("Classroom", x.Session.ClassroomCode),
                ("Teacher attendance", x.Session.TeacherAttendanceStatus), ("Session status", TeacherPresence.SessionStatus(x.Session.TeacherAttendanceStatus)),
                ("Reason", TeacherPresence.Reason(x.Session.TeacherAttendanceStatus)), ("Attendance", x.Student.Status),
                ("Check in", string.IsNullOrWhiteSpace(x.Student.CheckedInAt) ? "No check-in" : x.Student.CheckedInAt))));
            var gradeEvents = studentGrades.Where(x => selectedPeriods == null || selectedPeriods.Contains((student.Id, x.AcademicYear, x.Term))).Select(x =>
            {
                var enrollment = studentEnrollments.FirstOrDefault(item => item.AcademicYear == x.AcademicYear && item.Semester == x.Term);
                var gradeCode = enrollment is null
                    ? x.GradeCode
                    : codeFormat.PeriodLinkedWithConfiguredPrefix(enrollment.EnrollmentCode, enrollment.YearLevel, enrollment.Semester, "gradeManagementPrefix", "GRD");
                return (At: x.UpdatedAtUtc, Activity: Create(
                ("Activity", "Course grade"), ("CourseId", x.CourseId.ToString()), ("Grade code", gradeCode),
                ("Academic year", x.AcademicYear), ("Term", x.Term), ("Date", x.UpdatedAtUtc.ToString("yyyy-MM-dd")),
                ("Time", x.UpdatedAtUtc.ToString("HH:mm")), ("Course code", x.Course?.CourseCode ?? "—"),
                ("Course", x.Course?.Name ?? "Course"), ("Score", x.Score.ToString("0.##", CultureInfo.InvariantCulture)),
                ("Attendance score", x.AttendanceScore.ToString("0.##", CultureInfo.InvariantCulture)),
                ("Attendance maximum", x.AttendanceMaximum.ToString("0.##", CultureInfo.InvariantCulture)),
                ("Assignment score", x.AssignmentScore.ToString("0.##", CultureInfo.InvariantCulture)),
                ("Assignment maximum", x.AssignmentMaximum.ToString("0.##", CultureInfo.InvariantCulture)),
                ("Midterm score", x.MidtermScore.ToString("0.##", CultureInfo.InvariantCulture)),
                ("Midterm maximum", x.MidtermMaximum.ToString("0.##", CultureInfo.InvariantCulture)),
                ("Final exam score", x.FinalExamScore.ToString("0.##", CultureInfo.InvariantCulture)),
                ("Final exam maximum", x.FinalExamMaximum.ToString("0.##", CultureInfo.InvariantCulture)),
                ("Grade", x.LetterGrade)));
            });
            var events = enrollmentEvents.Concat(attendanceEvents).Concat(gradeEvents).OrderByDescending(x => x.At).ToList();
            var recordSource = studentEnrollments
                .OrderByDescending(x => x.AcademicYear)
                .ThenByDescending(x => x.Semester)
                .Select(x => codeFormat.PeriodLinked(x.EnrollmentCode, "student", "record", x.YearLevel, x.Semester))
                .FirstOrDefault() ?? student.StudentCode;
            return new OperationalRecordDto(
                student.Id,
                "Student",
                student.FullName,
                $"{student.StudentCode} · Year {student.YearLevel} · {student.Shift}",
                student.Status,
                $"{completed.Count} recorded class sessions · {studentGrades.Count} recorded course grades",
                events.Count == 0 ? null : events[0].At,
                events.Select(x => x.Activity).ToList(),
                Code: recordSource,
                PhotoDataUrl: student.PhotoDataUrl,
                Department: student.Department?.Name ?? "Unassigned",
                ResourceId: student.Id);
        }).ToList();
    }

    private static string SessionCode(ClassSessionRecord session)
    {
        if (session.ClassSessionRecordCode.StartsWith("SES-", StringComparison.OrdinalIgnoreCase) && session.ClassSessionRecordCode.Length <= 32)
            return session.ClassSessionRecordCode.ToUpperInvariant();
        var timetable = session.ScheduleEntry?.TimetableCode ?? session.ScheduleEntryId.ToString("N")[..8];
        var suffix = timetable.Contains('-') ? timetable[(timetable.IndexOf('-') + 1)..] : timetable;
        return $"SES-{session.SessionDate:yyyyMMdd}-{suffix}".ToUpperInvariant();
    }
}

public sealed record StudentOperationalRecordPage(IReadOnlyList<OperationalRecordDto> Records, int TotalCount);
