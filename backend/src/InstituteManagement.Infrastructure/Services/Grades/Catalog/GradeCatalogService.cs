using InstituteManagement.Application.Common.Pagination;
using InstituteManagement.Application.Features.Grades;
using InstituteManagement.Infrastructure.Services.Catalog;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Domain.Policies;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using InstituteManagement.Infrastructure.Services.Grades;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Grades;

public sealed class GradeCatalogService(InstituteDbContext db, InstituteCache cache) : CatalogFeatureBase<GradeResponseDto>(db, cache), IGradeCatalogService
{
    public override CatalogResource Resource => CatalogResource.Grades;
    public async Task<IReadOnlyList<GradeResponseDto>> GetAsync(string? search, Guid? departmentId, int? year, CancellationToken ct) =>
        (await GetAsync(search, departmentId, year, null, null, null, new PageRequest(1, 100), ct)).Items;

    public async Task<PagedResult<GradeResponseDto>> GetAsync(string? search, Guid? departmentId, int? year, Guid? teacherId, string? groupBy, string? status, PageRequest page, CancellationToken ct)
    {
        var period = await CurrentPeriodAsync(ct);
        var query = Db.GradeRecords.AsNoTracking()
            .Where(grade => (grade.AcademicYear == period.AcademicYear && grade.Term == period.Term || grade.SubmittedByTeacherId.HasValue)
                && grade.Student!.Status != "Inactive"
                && grade.Course!.IsActive
                && (!departmentId.HasValue || grade.Student.DepartmentId == departmentId)
                && (!teacherId.HasValue || grade.SubmittedByTeacherId == teacherId)
                && (!year.HasValue || Db.StudentEnrollments.Any(enrollment =>
                    enrollment.StudentId == grade.StudentId
                    && enrollment.AcademicYear == grade.AcademicYear
                    && enrollment.Semester == grade.Term
                    && enrollment.YearLevel == year.Value))
                && (grade.AcademicYear == period.AcademicYear && grade.Term == period.Term
                    || !Db.SemesterResultPublications.Any(publication => publication.StudentId == grade.StudentId && publication.AcademicYear == grade.AcademicYear && publication.Term == grade.Term)
                    || !Db.FinancialAccounts.Any(account => account.StudentId == grade.StudentId && account.AcademicYear == grade.AcademicYear && account.Semester == grade.Term && account.ClosedAtUtc.HasValue)));
        var term = search?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
            query = query.Where(grade =>
                grade.GradeCode.Contains(term)
                || grade.Student!.FullName.Contains(term)
                || grade.Student.StudentCode.Contains(term)
                || grade.Course!.Name.Contains(term)
                || grade.Course.CourseCode.Contains(term)
                || grade.LetterGrade.Contains(term)
                || Db.StudentEnrollments.Any(enrollment =>
                    enrollment.StudentId == grade.StudentId
                    && enrollment.AcademicYear == grade.AcademicYear
                    && enrollment.Semester == grade.Term
                    && enrollment.EnrollmentCode.Contains(term)));
        var grouping = groupBy?.Trim().ToLowerInvariant();
        var requestedStatus = status?.Trim();
        int totalCount;
        IQueryable<GradeRecord> pageQuery;
        HashSet<(Guid StudentId, string AcademicYear, string Term)>? studentGroups = null;
        HashSet<(Guid? TeacherId, Guid CourseId, string AcademicYear, string Term)>? courseGroups = null;
        if (grouping == "student")
        {
            var expectedCourseCountText = await Db.SystemSettings.AsNoTracking()
                .Where(setting => setting.Section == "grade-rules" && setting.Key == "expectedCourseCount")
                .Select(setting => setting.Value)
                .FirstOrDefaultAsync(ct);
            var expectedCourseCount = int.TryParse(expectedCourseCountText, out var configuredCourseCount) && configuredCourseCount > 0
                ? configuredCourseCount
                : SemesterResultRules.ExpectedCourseCount;
            var studentQuery = query.Where(grade => grade.SubmittedByTeacherId.HasValue
                && grade.SubmittedAtUtc.HasValue
                && grade.ReviewStatus != "SubmissionRequested"
                && grade.ReviewStatus != "SubmissionAuthorized");
            var groups = studentQuery.GroupBy(grade => new { grade.StudentId, grade.AcademicYear, grade.Term })
                .Select(group => new
                {
                    group.Key.StudentId,
                    group.Key.AcademicYear,
                    group.Key.Term,
                    StudentCode = group.Min(grade => grade.Student!.StudentCode),
                    CourseCount = group.Select(grade => grade.CourseId).Distinct().Count(),
                    ApprovedCount = group.Count(grade => grade.ReviewStatus == "Approved"),
                    FinalizedCount = group.Count(grade => grade.FinalizedAtUtc.HasValue)
                });
            groups = requestedStatus?.ToLowerInvariant() switch
            {
                "confirmed" => groups.Where(group => group.CourseCount >= expectedCourseCount && group.FinalizedCount == group.CourseCount),
                "ready" => groups.Where(group => group.CourseCount >= expectedCourseCount && group.ApprovedCount == group.CourseCount && group.FinalizedCount < group.CourseCount),
                "draft" => groups.Where(group => group.CourseCount < expectedCourseCount || group.ApprovedCount < group.CourseCount),
                _ => groups
            };
            totalCount = await groups.CountAsync(ct);
            var selected = await groups.OrderBy(group => group.AcademicYear).ThenBy(group => group.Term).ThenBy(group => group.StudentCode).ThenBy(group => group.StudentId)
                .Skip(page.Skip).Take(page.NormalizedPageSize).ToListAsync(ct);
            studentGroups = selected.Select(group => (group.StudentId, group.AcademicYear, group.Term)).ToHashSet();
            var selectedStudentIds = selected.Select(group => group.StudentId).Distinct().ToList();
            var academicYears = selected.Select(group => group.AcademicYear).Distinct().ToList();
            var terms = selected.Select(group => group.Term).Distinct().ToList();
            pageQuery = studentQuery.Where(grade => selectedStudentIds.Contains(grade.StudentId) && academicYears.Contains(grade.AcademicYear) && terms.Contains(grade.Term));
        }
        else if (grouping == "course")
        {
            var courseQuery = query.Where(grade => grade.SubmittedByTeacherId.HasValue);
            var groups = courseQuery
                .GroupBy(grade => new { grade.SubmittedByTeacherId, grade.CourseId, grade.AcademicYear, grade.Term })
                .Select(group => new
                {
                    group.Key.SubmittedByTeacherId,
                    group.Key.CourseId,
                    group.Key.AcademicYear,
                    group.Key.Term,
                    CourseCode = group.Min(grade => grade.Course!.CourseCode),
                    StatusPriority = group.Min(grade => grade.ReviewStatus == "SubmissionRequested" ? 0
                        : grade.ReviewStatus == "ResubmitRequested" ? 1
                        : grade.ReviewStatus == "Submitted" ? 2
                        : grade.ReviewStatus == "Pending" ? 3
                        : grade.ReviewStatus == "SubmissionAuthorized" ? 4
                        : grade.ReviewStatus == "ResubmitAuthorized" ? 5
                        : grade.ReviewStatus == "Rejected" ? 6
                        : grade.ReviewStatus == "Approved" ? 7 : 3)
                });
            var requestedPriority = requestedStatus?.ToLowerInvariant() switch
            {
                "submissionrequested" => 0,
                "resubmitrequested" => 1,
                "submitted" => 2,
                "pending" => 3,
                "submissionauthorized" => 4,
                "resubmitauthorized" => 5,
                "rejected" => 6,
                "approved" => 7,
                _ => -1
            };
            if (requestedStatus?.Equals("Action", StringComparison.OrdinalIgnoreCase) == true)
                groups = groups.Where(group => group.StatusPriority <= 3);
            else if (requestedPriority >= 0)
                groups = groups.Where(group => group.StatusPriority == requestedPriority);
            totalCount = await groups.CountAsync(ct);
            var selected = await groups.OrderBy(group => group.AcademicYear).ThenBy(group => group.Term).ThenBy(group => group.CourseCode).ThenBy(group => group.SubmittedByTeacherId).ThenBy(group => group.CourseId)
                .Skip(page.Skip).Take(page.NormalizedPageSize).ToListAsync(ct);
            courseGroups = selected.Select(group => (group.SubmittedByTeacherId, group.CourseId, group.AcademicYear, group.Term)).ToHashSet();
            var teacherIds = selected.Select(group => group.SubmittedByTeacherId).Distinct().ToList();
            var selectedCourseIds = selected.Select(group => group.CourseId).Distinct().ToList();
            var academicYears = selected.Select(group => group.AcademicYear).Distinct().ToList();
            var terms = selected.Select(group => group.Term).Distinct().ToList();
            pageQuery = courseQuery.Where(grade => teacherIds.Contains(grade.SubmittedByTeacherId) && selectedCourseIds.Contains(grade.CourseId) && academicYears.Contains(grade.AcademicYear) && terms.Contains(grade.Term));
        }
        else
        {
            totalCount = await query.CountAsync(ct);
            pageQuery = query
                .OrderBy(grade => grade.AcademicYear)
                .ThenBy(grade => grade.Term)
                .ThenBy(grade => grade.Student!.StudentCode)
                .ThenBy(grade => grade.Course!.CourseCode)
                .ThenBy(grade => grade.Id)
                .Skip(page.Skip)
                .Take(page.NormalizedPageSize);
        }
        var grades = await pageQuery
            .Include(grade => grade.Student)
                .ThenInclude(student => student!.Department)
            .Include(grade => grade.Course)
            .Include(grade => grade.SubmittedByTeacher)
            .ToListAsync(ct);
        if (studentGroups is not null)
            grades = grades.Where(grade => studentGroups.Contains((grade.StudentId, grade.AcademicYear, grade.Term))).ToList();
        if (courseGroups is not null)
            grades = grades.Where(grade => courseGroups.Contains((grade.SubmittedByTeacherId, grade.CourseId, grade.AcademicYear, grade.Term))).ToList();
        grades = grades.OrderBy(grade => grade.AcademicYear).ThenBy(grade => grade.Term).ThenBy(grade => grade.Student!.StudentCode).ThenBy(grade => grade.Course!.CourseCode).ThenBy(grade => grade.Id).ToList();
        if (grades.Count == 0) return PagedResult<GradeResponseDto>.Create([], page, totalCount);
        var courseIds = grades.Select(grade => grade.CourseId).Distinct().ToList();
        var sessions = await Db.ClassSessionRecords.AsNoTracking()
            .Where(session => session.AcademicYear == period.AcademicYear && session.Term == period.Term && courseIds.Contains(session.CourseId))
            .ToListAsync(ct);
        var sessionsByCourse = sessions.ToLookup(session => session.CourseId);
        var studentIds = grades.Select(grade => grade.StudentId).Distinct().ToList();
        var enrollments = (await Db.StudentEnrollments.AsNoTracking()
                .Where(enrollment => studentIds.Contains(enrollment.StudentId))
                .ToListAsync(ct))
            .GroupBy(enrollment => (enrollment.StudentId, enrollment.AcademicYear, enrollment.Semester))
            .ToDictionary(group => group.Key, group => group.OrderByDescending(enrollment => enrollment.CreateAt).First());
        var codeFormat = await BusinessCodeFormatter.LoadAsync(Db, ct);
        var responses = grades.Select(grade =>
            {
                enrollments.TryGetValue((grade.StudentId, grade.AcademicYear, grade.Term), out var enrollment);
                var displayCode = enrollment is null
                    ? grade.GradeCode
                    : codeFormat.PeriodLinkedWithConfiguredPrefix(enrollment.EnrollmentCode, enrollment.YearLevel, enrollment.Semester, "gradeManagementPrefix", "GRD");
                return Response(grade, sessionsByCourse[grade.CourseId], displayCode, enrollment);
            })
            .ToList();
        return PagedResult<GradeResponseDto>.Create(responses, page, totalCount);
    }

    public override Task<GradeResponseDto> CreateAsync(Dictionary<string, string> values, CancellationToken ct) =>
        throw new InvalidOperationException("Grades are generated automatically from students and cannot be added manually.");
    public override Task<GradeResponseDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken ct) =>
        throw new InvalidOperationException("Teacher grade submissions are read-only. Confirm or reject them in Assessment.");
    protected override async Task<Entity?> FindAsync(Guid id, CancellationToken ct) => await Db.GradeRecords.FindAsync([id], ct);
    protected override void Deactivate(Entity entity) => Db.Remove(entity);
    protected override async Task ValidateDeleteAsync(Entity entity, CancellationToken ct)
    {
        await Task.CompletedTask;
        throw new InvalidOperationException("Teacher grade submissions cannot be removed. Reject them in Assessment when correction is required.");
    }
    protected override GradeResponseDto Response(Guid id, IReadOnlyDictionary<string, string> values) =>
        new GradeResponseDto(id, new GradeValuesDto(
            Get(values, "gradeCode"),
            Get(values, "studentId"),
            Get(values, "student"),
            Get(values, "courseId"),
            Get(values, "course"),
            Get(values, "departmentId"),
            Get(values, "department"),
            Get(values, "attendanceScore"),
            Get(values, "attendanceMaximum", "10"),
            Get(values, "attendancePresent", "0"),
            Get(values, "attendanceSessions", "0"),
            Get(values, "assignmentScore"),
            Get(values, "assignmentMaximum", "20"),
            Get(values, "midtermScore"),
            Get(values, "midtermMaximum", "20"),
            Get(values, "finalExamScore"),
            Get(values, "finalExamMaximum", "50"),
            Get(values, "score"),
            Get(values, "grade"),
            Get(values, "academicYear"),
            Get(values, "term", "Semester 1"),
            Get(values, "submittedByTeacherId"),
            Get(values, "submittedByTeacher"),
            Get(values, "reviewStatus", "Pending"),
            Get(values, "reviewNote"),
            Get(values, "submissionVersion", "1"),
            Get(values, "submittedAtUtc"),
            Get(values, "reviewedAtUtc"),
            Get(values, "finalizedAtUtc"),
            Get(values, "createAt", DateTime.UtcNow.ToString("yyyy-MM-dd")),
            Get(values, "year"),
            Get(values, "shift"),
            Get(values, "resultCode")));

    private async Task<GradeRecord> BuildAsync(GradeRecord entity, Dictionary<string, string> values, CancellationToken ct)
    {
        var gradeCode = Required(values, "gradeCode");
        await EnsureUniqueAsync(Db.GradeRecords.Where(item => item.Id != entity.Id && item.GradeCode == gradeCode), "GradeCode", ct);
        entity.GradeCode = gradeCode;
        entity.StudentId = await RelatedIdAsync<Student>(values, "studentId", ct);
        entity.CourseId = await RelatedIdAsync<Course>(values, "courseId", ct);
        var student = await Db.Students.FindAsync([entity.StudentId], ct);
        var course = await Db.Courses.FindAsync([entity.CourseId], ct);
        if (student is null || course is null || !student.DepartmentId.HasValue
            || !StudentCurriculumPolicy.IncludesDepartment(student.YearLevel, student.DepartmentId.Value, course.DepartmentId)
            || student.Status == "Inactive" || !course.IsActive)
            throw new InvalidOperationException("Student and course must be active and match the Year 1 general curriculum or the Student's major.");
        values["student"] = student.FullName;
        values["course"] = course.Name;
        values["departmentId"] = student.DepartmentId?.ToString() ?? "";
        values["department"] = student.Department?.Name ?? "";
        var rules = await GradeCompositionCalculator.LoadRulesAsync(Db, ct);
        var assignment = DecimalInRange(values, "assignmentScore", 0, rules.Weights.Assignment);
        var midterm = DecimalInRange(values, "midtermScore", 0, rules.Weights.Midterm);
        var finalExam = DecimalInRange(values, "finalExamScore", 0, rules.Weights.FinalExam);
        var attendance = await GradeCompositionCalculator.AttendanceAsync(Db, entity.StudentId, entity.CourseId, entity.AcademicYear, entity.Term, rules.Weights.Attendance, rules.Attendance, ct);
        GradeCompositionCalculator.Apply(entity, rules.Weights, rules.Thresholds, attendance, assignment, midterm, finalExam);
        values["attendanceScore"] = entity.AttendanceScore.ToString("0.##");
        values["attendanceMaximum"] = entity.AttendanceMaximum.ToString("0.##");
        values["attendancePresent"] = attendance.Attended.ToString();
        values["attendanceSessions"] = attendance.Sessions.ToString();
        values["assignmentMaximum"] = entity.AssignmentMaximum.ToString("0.##");
        values["midtermMaximum"] = entity.MidtermMaximum.ToString("0.##");
        values["finalExamMaximum"] = entity.FinalExamMaximum.ToString("0.##");
        values["score"] = entity.Score.ToString("0.##");
        values["grade"] = entity.LetterGrade;
        var period = await CurrentPeriodAsync(ct);
        entity.AcademicYear = period.AcademicYear;
        entity.Term = period.Term;
        values["academicYear"] = entity.AcademicYear;
        values["term"] = entity.Term;
        await EnsureUniqueAsync(
            Db.GradeRecords.Where(grade => grade.Id != entity.Id && grade.StudentId == entity.StudentId && grade.CourseId == entity.CourseId && grade.AcademicYear == entity.AcademicYear && grade.Term == entity.Term),
            "Grade for this student, course, academic year, and term",
            ct);
        var reminders = await Db.SystemSettings.AsNoTracking().Where(x => x.Section == "notifications" && x.Key == "gradeReminders").Select(x => x.Value).FirstOrDefaultAsync(ct);
        if (entity.LetterGrade is "E" or "F" && (!bool.TryParse(reminders, out var enabled) || enabled)) Db.Notifications.Add(new Notification { Title = "Grade support reminder", Message = $"{student?.FullName ?? "Student"} received {entity.LetterGrade} in {course?.Name ?? "a course"}.", Severity = entity.LetterGrade == "F" ? "Warning" : "Info" });
        return entity;
    }
    private static GradeResponseDto Response(GradeRecord grade, IEnumerable<ClassSessionRecord> sessions, string displayCode, StudentEnrollment? enrollment)
    {
        var attendance = GradeCompositionCalculator.Attendance(
            sessions,
            grade.StudentId,
            grade.AttendanceMaximum);
        return new GradeResponseDto(grade.Id, new GradeValuesDto(
            displayCode,
            grade.StudentId.ToString(),
            grade.Student?.FullName ?? "—",
            grade.CourseId.ToString(),
            grade.Course?.Name ?? "—",
            grade.Student?.DepartmentId.ToString() ?? "",
            grade.Student?.Department?.Name ?? "—",
            grade.AttendanceScore.ToString("0.##"),
            grade.AttendanceMaximum.ToString("0.##"),
            attendance.Attended.ToString(),
            attendance.Sessions.ToString(),
            grade.AssignmentScore.ToString("0.##"),
            grade.AssignmentMaximum.ToString("0.##"),
            grade.MidtermScore.ToString("0.##"),
            grade.MidtermMaximum.ToString("0.##"),
            grade.FinalExamScore.ToString("0.##"),
            grade.FinalExamMaximum.ToString("0.##"),
            grade.Score.ToString("0.##"),
            grade.LetterGrade,
            grade.AcademicYear,
            grade.Term,
            grade.SubmittedByTeacherId?.ToString() ?? "",
            grade.SubmittedByTeacher?.FullName ?? "—",
            grade.ReviewStatus,
            grade.ReviewNote,
            grade.SubmissionVersion.ToString(),
            grade.SubmittedAtUtc?.ToString("O") ?? "",
            grade.ReviewedAtUtc?.ToString("O") ?? "",
            grade.FinalizedAtUtc?.ToString("O") ?? "",
            grade.CreateAt.ToString("yyyy-MM-dd"),
            enrollment?.YearLevel.ToString() ?? "",
            enrollment?.Shift ?? "",
            enrollment?.ResultCode ?? ""));
    }
    private async Task<(string AcademicYear, string Term)> CurrentPeriodAsync(CancellationToken ct)
    {
        var settings = await Db.SystemSettings.AsNoTracking().Where(x => (x.Section == "academic-year" && x.Key == "currentYear") || (x.Section == "semester" && x.Key == "currentTerm")).ToDictionaryAsync(x => $"{x.Section}:{x.Key}", x => x.Value, ct);
        return (settings.GetValueOrDefault("academic-year:currentYear", "2026\u20132027"), settings.GetValueOrDefault("semester:currentTerm", "Semester 1"));
    }
}
