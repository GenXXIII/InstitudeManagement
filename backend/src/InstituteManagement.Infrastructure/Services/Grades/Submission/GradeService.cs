using InstituteManagement.Application.Features.Grades;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Grades;

public sealed class GradeService(InstituteDbContext db, InstituteCache cache) : IGradeService
{
    public async Task SubmitAsync(Guid studentId, Guid courseId, decimal assignmentScore, decimal midtermScore, decimal finalExamScore, CancellationToken cancellationToken)
    {
        var teacherId = await db.TimetableEnrollments.AsNoTracking()
            .Where(item => item.CourseId == courseId && item.Status == "Active")
            .Select(item => item.TeacherId)
            .FirstOrDefaultAsync(cancellationToken);
        await SubmitCoreAsync(studentId, courseId, teacherId, assignmentScore, midtermScore, finalExamScore, false, cancellationToken);
    }

    public Task SubmitAsync(Guid studentId, Guid courseId, Guid teacherId, decimal assignmentScore, decimal midtermScore, decimal finalExamScore, CancellationToken cancellationToken) =>
        SubmitCoreAsync(studentId, courseId, teacherId, assignmentScore, midtermScore, finalExamScore, true, cancellationToken);

    public async Task RequestCourseSubmissionAsync(Guid teacherId, Guid courseId, IReadOnlyList<GradeStudentScore> students, CancellationToken cancellationToken)
    {
        if (teacherId == Guid.Empty) throw new ArgumentException("TeacherId is required.", nameof(teacherId));
        if (courseId == Guid.Empty) throw new ArgumentException("CourseId is required.", nameof(courseId));
        if (students.Count == 0) throw new ArgumentException("Every student in the assigned course roster is required.", nameof(students));
        if (students.Select(item => item.StudentId).Distinct().Count() != students.Count)
            throw new ArgumentException("A student cannot appear more than once in a course submission.", nameof(students));

        var teacher = await db.Teachers.FindAsync([teacherId], cancellationToken) ?? throw new KeyNotFoundException("Teacher not found.");
        var course = await db.Courses.FindAsync([courseId], cancellationToken) ?? throw new KeyNotFoundException("Course not found.");
        if (teacher.Status == "Inactive" || !course.IsActive)
            throw new InvalidOperationException("The Teacher and course must be active.");

        var period = await CurrentPeriodAsync(cancellationToken);
        var requestedIds = students.Select(item => item.StudentId).ToHashSet();
        var requestedEnrollments = await db.StudentEnrollments.AsNoTracking()
            .Include(item => item.Student)
            .Where(item => requestedIds.Contains(item.StudentId)
                && item.AcademicYear == period.AcademicYear
                && item.Semester == period.Term
                && item.Status == "Active")
            .ToListAsync(cancellationToken);
        if (requestedEnrollments.Count != requestedIds.Count || requestedEnrollments.Any(item => item.Student is null || item.Student.Status == "Inactive"))
            throw new InvalidOperationException("Every submitted student must have an active enrollment in the current academic period.");

        var cohort = requestedEnrollments[0];
        if (course.DepartmentId != cohort.DepartmentId || requestedEnrollments.Any(item =>
            item.DepartmentId != cohort.DepartmentId || item.YearLevel != cohort.YearLevel || item.Shift != cohort.Shift))
            throw new InvalidOperationException("A course submission must contain one complete department, year, and shift roster.");

        var assigned = await db.TimetableEnrollments.AsNoTracking()
            .Include(item => item.ScheduleEntry)
            .AnyAsync(item => item.TeacherId == teacherId
                && item.CourseId == courseId
                && item.YearLevel == cohort.YearLevel
                && item.AcademicYear == period.AcademicYear
                && item.Semester == period.Term
                && item.Status == "Active"
                && item.ScheduleEntry != null
                && item.ScheduleEntry.Shift == cohort.Shift,
                cancellationToken);
        if (!assigned) throw new InvalidOperationException("This Teacher is not assigned to this course roster.");

        var rosterIds = await db.StudentEnrollments.AsNoTracking()
            .Where(item => item.DepartmentId == cohort.DepartmentId
                && item.YearLevel == cohort.YearLevel
                && item.Shift == cohort.Shift
                && item.AcademicYear == period.AcademicYear
                && item.Semester == period.Term
                && item.Status == "Active"
                && item.Student != null
                && item.Student.Status != "Inactive")
            .Select(item => item.StudentId)
            .ToListAsync(cancellationToken);
        if (!requestedIds.SetEquals(rosterIds))
            throw new InvalidOperationException("Submit the entire assigned course roster in one request.");

        var existing = await db.GradeRecords
            .Where(item => requestedIds.Contains(item.StudentId)
                && item.CourseId == courseId
                && item.AcademicYear == period.AcademicYear
                && item.Term == period.Term)
            .ToDictionaryAsync(item => item.StudentId, cancellationToken);
        foreach (var grade in existing.Values.Where(item => item.SubmittedByTeacherId.HasValue && item.ReviewStatus != "Rejected"))
            throw new InvalidOperationException(CourseStateMessage(grade.ReviewStatus));

        var rules = await GradeCompositionCalculator.LoadRulesAsync(db, cancellationToken);
        var now = DateTime.UtcNow;
        GradeRecord? firstGrade = null;
        foreach (var score in students)
        {
            var imported = existing.GetValueOrDefault(score.StudentId);
            var grade = imported ?? new GradeRecord
            {
                GradeCode = await BusinessCodeFormatter.GenerateAsync(db, "grade", cancellationToken),
                StudentId = score.StudentId,
                CourseId = courseId,
                AcademicYear = period.AcademicYear,
                Term = period.Term,
                SubmissionVersion = 1,
            };
            if (imported is null) db.GradeRecords.Add(grade);
            var attendance = await GradeCompositionCalculator.AttendanceAsync(db, score.StudentId, courseId, period.AcademicYear, period.Term, rules.Weights.Attendance, cancellationToken);
            GradeCompositionCalculator.Apply(grade, rules.Weights, rules.Thresholds, attendance, score.AssignmentScore, score.MidtermScore, score.FinalExamScore);
            grade.SubmittedByTeacherId = teacherId;
            grade.ReviewStatus = "SubmissionRequested";
            grade.ReviewNote = string.Empty;
            grade.ReviewedAtUtc = null;
            grade.UpdatedAtUtc = now;
            firstGrade ??= grade;
        }

        db.AuditLogs.Add(new AuditLog
        {
            ResourceId = firstGrade!.Id,
            Type = "Grade assessment",
            Subject = course.Name,
            Action = "Course submission permission requested",
            Details = $"{teacher.FullName} · Year {cohort.YearLevel} · {cohort.Shift} · {students.Count} students · {period.AcademicYear} · {period.Term}",
        });
        db.Notifications.Add(new Notification
        {
            Title = "Course submission approval required",
            Message = $"{teacher.FullName} requested permission to submit {course.Name} results for {students.Count} students.",
            Severity = "Info",
        });
        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateDashboardAsync(cancellationToken);
    }

    public Task SubmitAuthorizedAsync(Guid gradeId, Guid teacherId, CancellationToken cancellationToken) =>
        SubmitAuthorizedCourseAsync(gradeId, teacherId, [], cancellationToken);

    public async Task SubmitAuthorizedCourseAsync(Guid gradeId, Guid teacherId, IReadOnlyList<GradeStudentScore> students, CancellationToken cancellationToken)
    {
        if (teacherId == Guid.Empty) throw new ArgumentException("TeacherId is required.", nameof(teacherId));
        var anchor = await GradeAsync(gradeId, cancellationToken);
        var grades = await CourseGradesAsync(anchor, cancellationToken);
        EnsureUniformStatus(grades);
        if (grades.Any(item => item.SubmittedByTeacherId != teacherId))
            throw new InvalidOperationException("Only the Teacher who requested permission can submit this course roster.");
        if (anchor.ReviewStatus is not ("SubmissionAuthorized" or "ResubmitAuthorized"))
            throw new InvalidOperationException("Administrator permission is required before this course roster can be submitted.");

        var isResubmission = anchor.ReviewStatus == "ResubmitAuthorized";
        if (isResubmission)
        {
            var scores = students.ToDictionary(item => item.StudentId);
            if (scores.Count != grades.Count || grades.Any(item => !scores.ContainsKey(item.StudentId)))
                throw new InvalidOperationException("Resubmit scores for the entire assigned course roster.");
            var rules = await GradeCompositionCalculator.LoadRulesAsync(db, cancellationToken);
            foreach (var grade in grades)
            {
                var score = scores[grade.StudentId];
                var attendance = await GradeCompositionCalculator.AttendanceAsync(db, grade.StudentId, grade.CourseId, grade.AcademicYear, grade.Term, rules.Weights.Attendance, cancellationToken);
                GradeCompositionCalculator.Apply(grade, rules.Weights, rules.Thresholds, attendance, score.AssignmentScore, score.MidtermScore, score.FinalExamScore);
                grade.SubmissionVersion++;
            }
        }

        var now = DateTime.UtcNow;
        foreach (var grade in grades)
        {
            grade.ReviewStatus = "Submitted";
            grade.ReviewNote = string.Empty;
            grade.SubmittedAtUtc = now;
            grade.ReviewedAtUtc = null;
            grade.UpdatedAtUtc = now;
        }
        var action = isResubmission ? "Course results resubmitted" : "Course results submitted";
        db.AuditLogs.Add(new AuditLog { ResourceId = anchor.Id, Type = "Grade assessment", Subject = anchor.Course?.Name ?? "Course", Action = action, Details = $"{grades.Count} students · version {grades.Max(item => item.SubmissionVersion)} · {anchor.SubmittedByTeacher?.FullName ?? "Teacher"}" });
        db.Notifications.Add(new Notification { Title = action, Message = $"{anchor.SubmittedByTeacher?.FullName ?? "A Teacher"} submitted the complete {anchor.Course?.Name ?? "course"} roster for final review.", Severity = "Info" });
        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateDashboardAsync(cancellationToken);
    }

    public async Task RequestCourseResubmissionAsync(Guid gradeId, Guid teacherId, string note, CancellationToken cancellationToken)
    {
        var anchor = await GradeAsync(gradeId, cancellationToken);
        var grades = await CourseGradesAsync(anchor, cancellationToken);
        EnsureUniformStatus(grades);
        if (grades.Any(item => item.SubmittedByTeacherId != teacherId))
            throw new InvalidOperationException("Only the assigned Teacher can request a new course submission.");
        if (anchor.ReviewStatus != "Approved")
            throw new InvalidOperationException("Only an accepted course submission can request resubmission permission.");

        var reason = string.IsNullOrWhiteSpace(note) ? "Teacher requested permission to update the accepted course results." : note.Trim();
        var now = DateTime.UtcNow;
        foreach (var grade in grades)
        {
            grade.ReviewStatus = "ResubmitRequested";
            grade.ReviewNote = reason;
            grade.ReviewedAtUtc = null;
            grade.UpdatedAtUtc = now;
        }
        db.AuditLogs.Add(new AuditLog { ResourceId = anchor.Id, Type = "Grade assessment", Subject = anchor.Course?.Name ?? "Course", Action = "Course resubmission permission requested", Details = $"{grades.Count} students · {reason}" });
        db.Notifications.Add(new Notification { Title = "Course resubmission approval required", Message = $"{anchor.SubmittedByTeacher?.FullName ?? "A Teacher"} requested permission to resubmit {anchor.Course?.Name ?? "course"} results.", Severity = "Info" });
        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateDashboardAsync(cancellationToken);
    }

    public async Task ReviewAsync(Guid gradeId, string decision, string note, CancellationToken cancellationToken)
    {
        var normalized = decision.Trim();
        var anchor = await GradeAsync(gradeId, cancellationToken);
        var grades = await CourseGradesAsync(anchor, cancellationToken);
        EnsureUniformStatus(grades);

        string nextStatus;
        string action;
        if (anchor.ReviewStatus == "SubmissionRequested")
        {
            if (normalized is not ("Approved" or "Rejected")) throw new ArgumentException("A course submission request can only be Approved or Rejected.", nameof(decision));
            if (normalized == "Rejected" && string.IsNullOrWhiteSpace(note)) throw new ArgumentException("A reason is required when a course submission request is rejected.", nameof(note));
            nextStatus = normalized == "Approved" ? "SubmissionAuthorized" : "Rejected";
            action = normalized == "Approved" ? "Course submission permission approved" : "Course submission request rejected";
        }
        else if (anchor.ReviewStatus == "ResubmitRequested")
        {
            if (normalized is not ("Approved" or "Rejected")) throw new ArgumentException("A course resubmission request can only be Approved or Rejected.", nameof(decision));
            if (normalized == "Rejected" && string.IsNullOrWhiteSpace(note)) throw new ArgumentException("A reason is required when a course resubmission request is rejected.", nameof(note));
            nextStatus = normalized == "Approved" ? "ResubmitAuthorized" : "Approved";
            action = normalized == "Approved" ? "Course resubmission permission approved" : "Course resubmission request rejected";
        }
        else if (anchor.ReviewStatus is "Submitted" or "Pending")
        {
            if (normalized is not ("Approved" or "Rejected" or "ResubmitRequested")) throw new ArgumentException("Submitted course results can only be Approved or Rejected.", nameof(decision));
            if (normalized != "Approved" && string.IsNullOrWhiteSpace(note)) throw new ArgumentException("A correction reason is required when course results are not accepted.", nameof(note));
            nextStatus = normalized;
            action = normalized == "Approved" ? "Course results accepted" : normalized == "Rejected" ? "Course results rejected" : "Course resubmission requested";
        }
        else throw new InvalidOperationException("This course submission is not waiting for an Administrator decision.");

        var now = DateTime.UtcNow;
        foreach (var grade in grades)
        {
            grade.ReviewStatus = nextStatus;
            grade.ReviewNote = note.Trim();
            grade.ReviewedAtUtc = now;
            grade.UpdatedAtUtc = now;
        }
        db.AuditLogs.Add(new AuditLog { ResourceId = anchor.Id, Type = "Grade assessment", Subject = anchor.Course?.Name ?? "Course", Action = action, Details = $"{grades.Count} students · version {grades.Max(item => item.SubmissionVersion)}{(string.IsNullOrWhiteSpace(note) ? "" : $" · {note.Trim()}")}" });
        db.Notifications.Add(new Notification { Title = action, Message = $"{anchor.Course?.Name ?? "Course"} · {grades.Count} students{(string.IsNullOrWhiteSpace(note) ? "" : $": {note.Trim()}")}", Severity = nextStatus is "Rejected" or "ResubmitRequested" ? "Warning" : "Info" });
        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateDashboardAsync(cancellationToken);
    }

    private async Task SubmitCoreAsync(Guid studentId, Guid courseId, Guid teacherId, decimal assignmentScore, decimal midtermScore, decimal finalExamScore, bool verifyTeacher, CancellationToken cancellationToken)
    {
        if (studentId == Guid.Empty) throw new ArgumentException("StudentId is required.", nameof(studentId));
        if (courseId == Guid.Empty) throw new ArgumentException("CourseId is required.", nameof(courseId));
        var student = await db.Students.FindAsync([studentId], cancellationToken) ?? throw new KeyNotFoundException("Student not found.");
        var course = await db.Courses.FindAsync([courseId], cancellationToken) ?? throw new KeyNotFoundException("Course not found.");
        if (student.Status == "Inactive" || !course.IsActive || student.DepartmentId != course.DepartmentId)
            throw new InvalidOperationException("Student and course must be active and belong to the same department.");

        Teacher? teacher = null;
        if (verifyTeacher)
        {
            if (teacherId == Guid.Empty) throw new ArgumentException("TeacherId is required.", nameof(teacherId));
            teacher = await db.Teachers.FindAsync([teacherId], cancellationToken) ?? throw new KeyNotFoundException("Teacher not found.");
            var studentEnrollment = await db.StudentEnrollments.AsNoTracking().Where(item => item.StudentId == studentId && item.Status == "Active").OrderByDescending(item => item.CreateAt).FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException("The student does not have an active enrollment.");
            var assignments = await db.TimetableEnrollments.AsNoTracking().Include(item => item.Course).Include(item => item.ScheduleEntry)
                .Where(item => item.TeacherId == teacherId && item.CourseId == courseId && item.Status == "Active").ToListAsync(cancellationToken);
            var assigned = assignments.Any(item => item.YearLevel == studentEnrollment.YearLevel && item.Course?.DepartmentId == studentEnrollment.DepartmentId && (item.ScheduleEntry is null || item.ScheduleEntry.Shift == studentEnrollment.Shift));
            if (!assigned) throw new InvalidOperationException("This Teacher is not assigned to this student's course, year, and shift.");
        }

        var period = await CurrentPeriodAsync(cancellationToken);
        var grade = await db.GradeRecords.FirstOrDefaultAsync(item => item.StudentId == studentId && item.CourseId == courseId && item.AcademicYear == period.AcademicYear && item.Term == period.Term, cancellationToken);
        var importedGrade = grade is not null && verifyTeacher && !grade.SubmittedByTeacherId.HasValue;
        var isResubmission = grade?.ReviewStatus == "ResubmitRequested";
        if (grade is null)
        {
            grade = new GradeRecord { GradeCode = await BusinessCodeFormatter.GenerateAsync(db, "grade", cancellationToken), StudentId = studentId, CourseId = courseId, AcademicYear = period.AcademicYear, Term = period.Term, SubmissionVersion = 1 };
            db.GradeRecords.Add(grade);
        }
        else if (!importedGrade)
        {
            if (grade.ReviewStatus is not ("Rejected" or "ResubmitRequested")) throw new InvalidOperationException(CourseStateMessage(grade.ReviewStatus));
            if (isResubmission) grade.SubmissionVersion++;
        }
        else grade.SubmissionVersion = Math.Max(1, grade.SubmissionVersion);

        var rules = await GradeCompositionCalculator.LoadRulesAsync(db, cancellationToken);
        var attendance = await GradeCompositionCalculator.AttendanceAsync(db, studentId, courseId, period.AcademicYear, period.Term, rules.Weights.Attendance, cancellationToken);
        GradeCompositionCalculator.Apply(grade, rules.Weights, rules.Thresholds, attendance, assignmentScore, midtermScore, finalExamScore);
        grade.SubmittedByTeacherId = teacherId == Guid.Empty ? null : teacherId;
        grade.ReviewStatus = isResubmission ? "Submitted" : "SubmissionRequested";
        grade.ReviewNote = string.Empty;
        grade.SubmittedAtUtc = isResubmission ? DateTime.UtcNow : null;
        grade.ReviewedAtUtc = null;
        grade.UpdatedAtUtc = DateTime.UtcNow;
        db.AuditLogs.Add(new AuditLog { ResourceId = grade.Id, Type = "Grade assessment", Subject = student.FullName, Action = isResubmission ? "Submitted again for final review" : "Submission permission requested", Details = $"{course.Name}: {grade.Score:0.##}/100 ({grade.LetterGrade}) · version {grade.SubmissionVersion} · {teacher?.FullName ?? "Teacher"} · {period.AcademicYear} · {period.Term}" });
        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateDashboardAsync(cancellationToken);
    }

    private async Task<GradeRecord> GradeAsync(Guid gradeId, CancellationToken cancellationToken) =>
        await db.GradeRecords.AsNoTracking().Include(item => item.Student).Include(item => item.Course).Include(item => item.SubmittedByTeacher)
            .FirstOrDefaultAsync(item => item.Id == gradeId, cancellationToken) ?? throw new KeyNotFoundException("Course submission not found.");

    private async Task<List<GradeRecord>> CourseGradesAsync(GradeRecord anchor, CancellationToken cancellationToken)
    {
        var enrollment = await db.StudentEnrollments.AsNoTracking().FirstOrDefaultAsync(item =>
            item.StudentId == anchor.StudentId && item.AcademicYear == anchor.AcademicYear && item.Semester == anchor.Term && item.Status == "Active", cancellationToken);
        IQueryable<GradeRecord> query = db.GradeRecords.AsTracking().Include(item => item.Student).Include(item => item.Course).Include(item => item.SubmittedByTeacher)
            .Where(item => item.CourseId == anchor.CourseId && item.SubmittedByTeacherId == anchor.SubmittedByTeacherId && item.AcademicYear == anchor.AcademicYear && item.Term == anchor.Term);
        if (enrollment is not null)
        {
            var studentIds = await db.StudentEnrollments.AsNoTracking().Where(item =>
                item.DepartmentId == enrollment.DepartmentId && item.YearLevel == enrollment.YearLevel && item.Shift == enrollment.Shift
                && item.AcademicYear == enrollment.AcademicYear && item.Semester == enrollment.Semester && item.Status == "Active")
                .Select(item => item.StudentId)
                .ToListAsync(cancellationToken);
            query = query.Where(item => studentIds.Contains(item.StudentId));
        }
        var grades = await query.ToListAsync(cancellationToken);
        return grades.Count == 0 ? [anchor] : grades;
    }

    private static void EnsureUniformStatus(IReadOnlyList<GradeRecord> grades)
    {
        if (grades.Select(item => item.ReviewStatus).Distinct().Count() != 1)
            throw new InvalidOperationException("The course roster has inconsistent workflow states. Refresh it before continuing.");
    }

    private async Task<(string AcademicYear, string Term)> CurrentPeriodAsync(CancellationToken cancellationToken)
    {
        var period = await db.SystemSettings.AsNoTracking()
            .Where(item => (item.Section == "academic-year" && item.Key == "currentYear") || (item.Section == "semester" && item.Key == "currentTerm"))
            .ToDictionaryAsync(item => $"{item.Section}:{item.Key}", item => item.Value, cancellationToken);
        return (period.GetValueOrDefault("academic-year:currentYear", "2026–2027"), period.GetValueOrDefault("semester:currentTerm", "Semester 1"));
    }

    private static string CourseStateMessage(string status) => status switch
    {
        "Approved" => "This course submission is accepted. Request resubmission permission before changing it.",
        "SubmissionAuthorized" => "Submission permission is approved. Submit the complete saved course roster.",
        "ResubmitAuthorized" => "Resubmission permission is approved. Resubmit the complete corrected course roster.",
        "Submitted" or "Pending" => "This course roster is waiting for Administrator final review.",
        "SubmissionRequested" => "This course submission request is waiting for Administrator approval.",
        "ResubmitRequested" => "This course resubmission request is waiting for Administrator approval.",
        _ => "This course roster cannot be edited in its current state.",
    };
}
