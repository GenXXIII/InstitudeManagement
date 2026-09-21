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

        var period = await db.SystemSettings.AsNoTracking().Where(item => (item.Section == "academic-year" && item.Key == "currentYear") || (item.Section == "semester" && item.Key == "currentTerm")).ToDictionaryAsync(item => $"{item.Section}:{item.Key}", item => item.Value, cancellationToken);
        var academicYear = period.GetValueOrDefault("academic-year:currentYear", "2026–2027");
        var currentTerm = period.GetValueOrDefault("semester:currentTerm", "Semester 1");
        var grade = await db.GradeRecords.FirstOrDefaultAsync(item => item.StudentId == studentId && item.CourseId == courseId && item.AcademicYear == academicYear && item.Term == currentTerm, cancellationToken);
        if (grade is null)
        {
            grade = new GradeRecord { GradeCode = await BusinessCodeFormatter.GenerateAsync(db, "grade", cancellationToken), StudentId = studentId, CourseId = courseId, AcademicYear = academicYear, Term = currentTerm, SubmissionVersion = 1 };
            db.GradeRecords.Add(grade);
        }
        else
        {
            if (grade.ReviewStatus != "ResubmitAuthorized")
            {
                var message = grade.ReviewStatus switch
                {
                    "Approved" => "This grade is approved and cannot be resubmitted.",
                    "Rejected" => "Request Administrator permission before refilling this rejected score.",
                    "ResubmitRequested" => "The resubmission request is waiting for Administrator permission.",
                    _ => "This grade is waiting for Administrator review."
                };
                throw new InvalidOperationException(message);
            }
            grade.SubmissionVersion++;
        }

        var rules = await GradeCompositionCalculator.LoadRulesAsync(db, cancellationToken);
        var attendance = await GradeCompositionCalculator.AttendanceAsync(db, studentId, courseId, academicYear, currentTerm, rules.Weights.Attendance, cancellationToken);
        GradeCompositionCalculator.Apply(grade, rules.Weights, rules.Thresholds, attendance, assignmentScore, midtermScore, finalExamScore);
        grade.SubmittedByTeacherId = teacherId == Guid.Empty ? null : teacherId;
        grade.ReviewStatus = "Pending";
        grade.ReviewNote = string.Empty;
        grade.SubmittedAtUtc = DateTime.UtcNow;
        grade.ReviewedAtUtc = null;
        grade.UpdatedAtUtc = DateTime.UtcNow;
        db.AuditLogs.Add(new AuditLog { ResourceId = grade.Id, Type = "Grade assessment", Subject = student.FullName, Action = grade.SubmissionVersion == 1 ? "Submitted for review" : "Resubmitted for review", Details = $"{course.Name}: {grade.Score:0.##}/100 ({grade.LetterGrade}) · version {grade.SubmissionVersion} · {teacher?.FullName ?? "Teacher"} · {academicYear} · {currentTerm}" });
        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateDashboardAsync(cancellationToken);
    }

    public async Task ReviewAsync(Guid gradeId, string decision, string note, CancellationToken cancellationToken)
    {
        var normalized = decision.Trim();
        if (normalized is not ("Approved" or "Rejected")) throw new ArgumentException("Decision must be Approved or Rejected.", nameof(decision));
        if (normalized == "Rejected" && string.IsNullOrWhiteSpace(note)) throw new ArgumentException("A correction note is required when a grade is rejected.", nameof(note));
        var grade = await db.GradeRecords.Include(item => item.Student).Include(item => item.Course).FirstOrDefaultAsync(item => item.Id == gradeId, cancellationToken)
            ?? throw new KeyNotFoundException("Grade submission not found.");
        if (grade.ReviewStatus != "Pending") throw new InvalidOperationException("Only a pending grade submission can be reviewed.");
        grade.ReviewStatus = normalized;
        grade.ReviewNote = note.Trim();
        grade.ReviewedAtUtc = DateTime.UtcNow;
        grade.UpdatedAtUtc = DateTime.UtcNow;
        db.AuditLogs.Add(new AuditLog { ResourceId = grade.Id, Type = "Grade assessment", Subject = grade.Student?.FullName ?? "Student", Action = normalized, Details = $"{grade.Course?.Name ?? "Course"} · version {grade.SubmissionVersion}{(string.IsNullOrWhiteSpace(note) ? "" : $" · {note.Trim()}")}" });
        if (normalized == "Rejected") db.Notifications.Add(new Notification { Title = "Grade correction requested", Message = $"{grade.Student?.FullName ?? "A student"} · {grade.Course?.Name ?? "Course"}: {note.Trim()}", Severity = "Warning" });
        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateDashboardAsync(cancellationToken);
    }

    public async Task RequestResubmissionAsync(Guid gradeId, Guid teacherId, CancellationToken cancellationToken)
    {
        if (teacherId == Guid.Empty) throw new ArgumentException("TeacherId is required.", nameof(teacherId));
        var grade = await db.GradeRecords.Include(item => item.Student).Include(item => item.Course).Include(item => item.SubmittedByTeacher)
            .FirstOrDefaultAsync(item => item.Id == gradeId, cancellationToken) ?? throw new KeyNotFoundException("Grade submission not found.");
        if (grade.SubmittedByTeacherId != teacherId) throw new InvalidOperationException("Only the Teacher who submitted this grade can request permission to resubmit it.");
        if (grade.ReviewStatus != "Rejected") throw new InvalidOperationException("Resubmission permission can only be requested for a rejected grade.");
        grade.ReviewStatus = "ResubmitRequested";
        grade.UpdatedAtUtc = DateTime.UtcNow;
        db.AuditLogs.Add(new AuditLog { ResourceId = grade.Id, Type = "Grade assessment", Subject = grade.Student?.FullName ?? "Student", Action = "Resubmission permission requested", Details = $"{grade.Course?.Name ?? "Course"} · version {grade.SubmissionVersion} · {grade.SubmittedByTeacher?.FullName ?? "Teacher"}" });
        db.Notifications.Add(new Notification { Title = "Grade resubmission permission", Message = $"{grade.SubmittedByTeacher?.FullName ?? "A Teacher"} requested permission to refill {grade.Student?.FullName ?? "a student"}'s {grade.Course?.Name ?? "course"} score.", Severity = "Warning" });
        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateDashboardAsync(cancellationToken);
    }

    public async Task AuthorizeResubmissionAsync(Guid gradeId, CancellationToken cancellationToken)
    {
        var grade = await db.GradeRecords.Include(item => item.Student).Include(item => item.Course).Include(item => item.SubmittedByTeacher)
            .FirstOrDefaultAsync(item => item.Id == gradeId, cancellationToken) ?? throw new KeyNotFoundException("Grade submission not found.");
        if (grade.ReviewStatus != "ResubmitRequested") throw new InvalidOperationException("The Teacher has not requested resubmission permission for this grade.");
        grade.ReviewStatus = "ResubmitAuthorized";
        grade.ReviewedAtUtc = DateTime.UtcNow;
        grade.UpdatedAtUtc = DateTime.UtcNow;
        db.AuditLogs.Add(new AuditLog { ResourceId = grade.Id, Type = "Grade assessment", Subject = grade.Student?.FullName ?? "Student", Action = "Resubmission authorized", Details = $"{grade.Course?.Name ?? "Course"} · version {grade.SubmissionVersion}" });
        db.Notifications.Add(new Notification { Title = "Grade refill permitted", Message = $"Administrator permitted {grade.SubmittedByTeacher?.FullName ?? "the Teacher"} to refill and resubmit {grade.Student?.FullName ?? "a student"}'s {grade.Course?.Name ?? "course"} score.", Severity = "Info" });
        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateDashboardAsync(cancellationToken);
    }
}
