using InstituteManagement.Application.Common.Pagination;
using InstituteManagement.Application.Features.Results;
using InstituteManagement.Application.Features.Enrollment.Students.Progression;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Domain.Policies;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Grades;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Results;

public sealed class ResultQueryService(InstituteDbContext db, IStudentEnrollmentProgression progression) : IResultQueryService
{
    public async Task<IReadOnlyList<SemesterResultDto>> GetAsync(Guid? departmentId, int? year, string? semester, string? academicYear, bool history, CancellationToken cancellationToken) =>
        (await GetAsync(departmentId, year, semester, academicYear, history, null, false, null, null, new PageRequest(1, 100), cancellationToken)).Items;

    public async Task<IReadOnlyList<SemesterResultDto>> GetAsync(Guid? departmentId, int? year, string? semester, string? academicYear, bool history, Guid? studentId, bool publishedOnly, CancellationToken cancellationToken) =>
        (await GetAsync(departmentId, year, semester, academicYear, history, studentId, publishedOnly, null, null, new PageRequest(1, 100), cancellationToken)).Items;

    public async Task<PagedResult<SemesterResultDto>> GetAsync(Guid? departmentId, int? year, string? semester, string? academicYear, bool history, Guid? studentId, bool publishedOnly, string? search, string? outcome, PageRequest page, CancellationToken cancellationToken)
    {
        var settings = await db.SystemSettings.AsNoTracking().Where(item => item.Section == "academic-year" || item.Section == "semester" || item.Section == "grade-rules" || item.Section == "attendance-rules").ToListAsync(cancellationToken);
        var currentAcademicYear = academicYear ?? settings.FirstOrDefault(item => item.Section == "academic-year" && item.Key == "currentYear")?.Value ?? "2026–2027";
        var currentSemester = semester ?? settings.FirstOrDefault(item => item.Section == "semester" && item.Key == "currentTerm")?.Value ?? "Semester 1";
        var gradeSettings = settings.Where(item => item.Section == "grade-rules").ToDictionary(item => item.Key, item => item.Value);
        var attendanceSettings = settings.Where(item => item.Section == "attendance-rules").ToDictionary(item => item.Key, item => item.Value);
        var thresholds = GradeThresholds.From(gradeSettings);
        var weights = GradeWeights.From(gradeSettings);
        var attendanceRules = AttendanceResultRules.From(attendanceSettings);
        var expectedCourseCount = ExpectedCourseCount(gradeSettings);

        IQueryable<ResultPeriodCandidate> candidateQuery;
        if (history || publishedOnly)
        {
            candidateQuery = db.SemesterResultPublications.AsNoTracking()
                .Where(publication => !history
                    || (publication.AcademicYear != currentAcademicYear || publication.Term != currentSemester)
                    && db.FinancialAccounts.Any(account =>
                        account.StudentId == publication.StudentId
                        && account.AcademicYear == publication.AcademicYear
                        && account.Semester == publication.Term
                        && account.ClosedAtUtc.HasValue))
                .Select(publication => new ResultPeriodCandidate { StudentId = publication.StudentId, AcademicYear = publication.AcademicYear, Semester = publication.Term });
        }
        else
        {
            var completed = db.GradeRecords.AsNoTracking()
                .Where(grade => grade.FinalizedAtUtc.HasValue && (grade.ReviewStatus == "Approved" || grade.SubmittedByTeacherId == null))
                .GroupBy(grade => new { grade.StudentId, grade.AcademicYear, grade.Term })
                .Where(group => group.Select(grade => grade.CourseId).Distinct().Count() == expectedCourseCount
                    && (group.Key.AcademicYear == currentAcademicYear && group.Key.Term == currentSemester
                        || !db.SemesterResultPublications.Any(publication => publication.StudentId == group.Key.StudentId && publication.AcademicYear == group.Key.AcademicYear && publication.Term == group.Key.Term)
                        || !db.FinancialAccounts.Any(account => account.StudentId == group.Key.StudentId && account.AcademicYear == group.Key.AcademicYear && account.Semester == group.Key.Term && account.ClosedAtUtc.HasValue)))
                .Select(group => new { group.Key.StudentId, AcademicYear = group.Key.AcademicYear, Semester = group.Key.Term });
            var currentEnrollments = db.StudentEnrollments.AsNoTracking()
                .Where(enrollment => enrollment.Status == "Active" && enrollment.AcademicYear == currentAcademicYear && enrollment.Semester == currentSemester)
                .Select(enrollment => new { enrollment.StudentId, enrollment.AcademicYear, enrollment.Semester });
            candidateQuery = completed.Concat(currentEnrollments).Distinct()
                .Select(candidate => new ResultPeriodCandidate { StudentId = candidate.StudentId, AcademicYear = candidate.AcademicYear, Semester = candidate.Semester });
        }

        var searchTerm = search?.Trim();
        candidateQuery = candidateQuery.Where(candidate =>
            (string.IsNullOrWhiteSpace(semester) || candidate.Semester == semester)
            && (string.IsNullOrWhiteSpace(academicYear) || candidate.AcademicYear == academicYear)
            && db.Students.Any(item => item.Id == candidate.StudentId
                && (!departmentId.HasValue || item.DepartmentId == departmentId)
                && (!year.HasValue || item.YearLevel == year)
                && (!studentId.HasValue || item.Id == studentId)
                && (history || publishedOnly || item.Status != "Inactive")
                && (string.IsNullOrWhiteSpace(searchTerm)
                    || item.FullName.Contains(searchTerm)
                    || item.StudentCode.Contains(searchTerm)
                    || item.Shift.Contains(searchTerm)
                    || item.Department!.Name.Contains(searchTerm)
                    || db.StudentEnrollments.Any(enrollment => enrollment.StudentId == item.Id
                        && enrollment.AcademicYear == candidate.AcademicYear
                        && enrollment.Semester == candidate.Semester
                        && (enrollment.EnrollmentCode.Contains(searchTerm) || enrollment.ResultCode.Contains(searchTerm)))
                    || db.GradeRecords.Any(grade => grade.StudentId == item.Id
                        && grade.AcademicYear == candidate.AcademicYear
                        && grade.Term == candidate.Semester
                        && (grade.GradeCode.Contains(searchTerm) || grade.Course!.CourseCode.Contains(searchTerm) || grade.Course.Name.Contains(searchTerm))))));

        var normalizedOutcome = outcome?.Trim().ToLowerInvariant();
        if (normalizedOutcome is "pass" or "retake" or "fail" or "pending")
        {
            var statistics = candidateQuery.Select(candidate => new
            {
                Candidate = candidate,
                SessionCount = db.ClassSessionStudentAttendance.Count(item => item.StudentId == candidate.StudentId
                    && item.ClassSessionRecord != null
                    && item.ClassSessionRecord.AcademicYear == candidate.AcademicYear
                    && item.ClassSessionRecord.Term == candidate.Semester),
                SessionAbsent = db.ClassSessionStudentAttendance.Count(item => item.StudentId == candidate.StudentId
                    && item.ClassSessionRecord != null
                    && item.ClassSessionRecord.AcademicYear == candidate.AcademicYear
                    && item.ClassSessionRecord.Term == candidate.Semester
                    && item.Status == "Absent"),
                SessionPermission = db.ClassSessionStudentAttendance.Count(item => item.StudentId == candidate.StudentId
                    && item.ClassSessionRecord != null
                    && item.ClassSessionRecord.AcademicYear == candidate.AcademicYear
                    && item.ClassSessionRecord.Term == candidate.Semester
                    && (item.Status == "Excused" || item.Status == "Permission")),
                LegacyAbsent = db.AttendanceRecords.Count(item => item.StudentId == candidate.StudentId
                    && item.AcademicYear == candidate.AcademicYear
                    && item.Term == candidate.Semester
                    && item.Status == "Absent"),
                LegacyPermission = db.AttendanceRecords.Count(item => item.StudentId == candidate.StudentId
                    && item.AcademicYear == candidate.AcademicYear
                    && item.Term == candidate.Semester
                    && (item.Status == "Excused" || item.Status == "Permission")),
                FinalizedCourseCount = db.GradeRecords
                    .Where(grade => grade.StudentId == candidate.StudentId
                        && grade.AcademicYear == candidate.AcademicYear
                        && grade.Term == candidate.Semester
                        && grade.FinalizedAtUtc.HasValue
                        && (grade.ReviewStatus == "Approved" || grade.SubmittedByTeacherId == null))
                    .Select(grade => grade.CourseId)
                    .Distinct()
                    .Count(),
                HasFailingGrade = db.GradeRecords.Any(grade => grade.StudentId == candidate.StudentId
                    && grade.AcademicYear == candidate.AcademicYear
                    && grade.Term == candidate.Semester
                    && grade.FinalizedAtUtc.HasValue
                    && (grade.ReviewStatus == "Approved" || grade.SubmittedByTeacherId == null)
                    && grade.LetterGrade == "F")
            }).Select(row => new
            {
                row.Candidate,
                Absent = row.SessionCount > 0 ? row.SessionAbsent : row.LegacyAbsent,
                Permission = row.SessionCount > 0 ? row.SessionPermission : row.LegacyPermission,
                row.FinalizedCourseCount,
                row.HasFailingGrade
            });
            statistics = normalizedOutcome switch
            {
                "fail" => statistics.Where(row => row.Absent >= attendanceRules.FailAbsentSections || row.Permission >= attendanceRules.FailPermissionSections),
                "retake" => statistics.Where(row => row.Absent < attendanceRules.FailAbsentSections
                    && row.Permission < attendanceRules.FailPermissionSections
                    && (row.Absent >= attendanceRules.RetakeAbsentSections
                        || row.Permission >= attendanceRules.RetakePermissionSections
                        || row.FinalizedCourseCount == expectedCourseCount && row.HasFailingGrade)),
                "pending" => statistics.Where(row => row.Absent < attendanceRules.FailAbsentSections
                    && row.Permission < attendanceRules.FailPermissionSections
                    && row.Absent < attendanceRules.RetakeAbsentSections
                    && row.Permission < attendanceRules.RetakePermissionSections
                    && row.FinalizedCourseCount != expectedCourseCount),
                _ => statistics.Where(row => row.Absent < attendanceRules.FailAbsentSections
                    && row.Permission < attendanceRules.FailPermissionSections
                    && row.Absent < attendanceRules.RetakeAbsentSections
                    && row.Permission < attendanceRules.RetakePermissionSections
                    && row.FinalizedCourseCount == expectedCourseCount
                    && !row.HasFailingGrade)
            };
            candidateQuery = statistics.Select(row => row.Candidate);
        }

        var totalCount = await candidateQuery.CountAsync(cancellationToken);
        var candidates = await candidateQuery
            .Join(db.Students.AsNoTracking(), candidate => candidate.StudentId, student => student.Id, (candidate, student) => new { Candidate = candidate, student.YearLevel, student.Shift, student.FullName })
            .OrderBy(row => row.YearLevel)
            .ThenBy(row => row.Shift == "Morning" ? 0 : row.Shift == "Afternoon" ? 1 : row.Shift == "Evening" ? 2 : row.Shift == "Weekend" ? 3 : 4)
            .ThenBy(row => row.FullName)
            .ThenByDescending(row => row.Candidate.AcademicYear)
            .ThenBy(row => row.Candidate.Semester)
            .ThenBy(row => row.Candidate.StudentId)
            .Skip(page.Skip)
            .Take(page.NormalizedPageSize)
            .Select(row => row.Candidate)
            .ToListAsync(cancellationToken);
        if (candidates.Count == 0) return PagedResult<SemesterResultDto>.Create([], page, totalCount);

        var selectedStudentIds = candidates.Select(item => item.StudentId).Distinct().ToList();
        var candidateYears = candidates.Select(item => item.AcademicYear).Distinct().ToList();
        var candidateTerms = candidates.Select(item => item.Semester).Distinct().ToList();
        var students = await db.Students.AsNoTracking().Include(item => item.Department)
            .Where(item => selectedStudentIds.Contains(item.Id))
            .ToListAsync(cancellationToken);
        var studentIds = students.Select(item => item.Id).ToHashSet();
        var enrollments = await db.StudentEnrollments.AsNoTracking()
            .Include(item => item.Department)
            .Where(item => studentIds.Contains(item.StudentId) && item.Status != "Removed" && candidateYears.Contains(item.AcademicYear) && candidateTerms.Contains(item.Semester))
            .ToListAsync(cancellationToken);
        var attendance = await db.AttendanceRecords.AsNoTracking()
            .Where(item => studentIds.Contains(item.StudentId) && candidateYears.Contains(item.AcademicYear) && candidateTerms.Contains(item.Term))
            .ToListAsync(cancellationToken);
        var allGrades = await db.GradeRecords.AsNoTracking().Include(item => item.Course)
            .Where(item => studentIds.Contains(item.StudentId) && candidateYears.Contains(item.AcademicYear) && candidateTerms.Contains(item.Term))
            .ToListAsync(cancellationToken);
        var finalizedGrades = allGrades.Where(item => item.FinalizedAtUtc.HasValue && (item.ReviewStatus == "Approved" || item.SubmittedByTeacherId == null)).ToList();
        var courseAssignments = await db.CourseAssignments.AsNoTracking()
            .Include(item => item.Course)
            .Where(item => item.Status != "Removed" && candidateYears.Contains(item.AcademicYear) && candidateTerms.Contains(item.Semester))
            .ToListAsync(cancellationToken);
        var timetableAssignments = await db.TimetableEnrollments.AsNoTracking()
            .Include(item => item.Course)
            .Include(item => item.ScheduleEntry)
            .Where(item => item.Status == "Active" && candidateYears.Contains(item.AcademicYear) && candidateTerms.Contains(item.Semester))
            .ToListAsync(cancellationToken);
        var catalogCourses = await db.Courses.AsNoTracking().Where(item => item.IsActive).ToListAsync(cancellationToken);
        var publications = await db.SemesterResultPublications.AsNoTracking().Where(item => studentIds.Contains(item.StudentId) && candidateYears.Contains(item.AcademicYear) && candidateTerms.Contains(item.Term)).ToListAsync(cancellationToken);
        var sessionAttendance = await db.ClassSessionStudentAttendance.AsNoTracking()
            .Where(item => studentIds.Contains(item.StudentId)
                && item.ClassSessionRecord != null
                && candidateYears.Contains(item.ClassSessionRecord.AcademicYear)
                && candidateTerms.Contains(item.ClassSessionRecord.Term)
                && (!departmentId.HasValue || item.ClassSessionRecord.DepartmentId == departmentId)
                && (!year.HasValue || item.ClassSessionRecord.YearLevel == year))
            .Select(item => new SessionAttendance(item.StudentId, item.ClassSessionRecord!.AcademicYear, item.ClassSessionRecord.Term, item.Status))
            .ToListAsync(cancellationToken);
        var candidatePeriods = candidates.ToLookup(item => item.StudentId, item => new Period(item.AcademicYear, item.Semester));
        var codeFormat = await BusinessCodeFormatter.LoadAsync(db, cancellationToken);
        var results = new List<SemesterResultDto>();

        foreach (var student in students)
        {
            foreach (var period in candidatePeriods[student.Id])
            {
                if (history && period.AcademicYear == currentAcademicYear && period.Semester == currentSemester) continue;
                var publication = publications.FirstOrDefault(item => item.StudentId == student.Id && item.AcademicYear == period.AcademicYear && item.Term == period.Semester);
                if ((history || publishedOnly) && publication is null) continue;
                var enrollment = enrollments
                    .Where(item => item.StudentId == student.Id && item.AcademicYear == period.AcademicYear && item.Semester == period.Semester)
                    .OrderByDescending(item => item.Status == "Active")
                    .ThenByDescending(item => item.UpdatedAtUtc)
                    .FirstOrDefault();
                var resultDepartmentId = enrollment?.DepartmentId ?? student.DepartmentId ?? Guid.Empty;
                var resultDepartment = enrollment?.Department?.Name ?? student.Department?.Name ?? "Unassigned";
                var resultYear = enrollment?.YearLevel ?? student.YearLevel;
                var resultShift = enrollment?.Shift ?? student.Shift;
                var periodAttendance = attendance.Where(item => item.StudentId == student.Id && item.AcademicYear == period.AcademicYear && item.Term == period.Semester).ToList();
                var timetableAttendance = sessionAttendance.Where(item => item.StudentId == student.Id && item.AcademicYear == period.AcademicYear && item.Semester == period.Semester).ToList();
                var submittedGrades = allGrades.Where(item => item.StudentId == student.Id && item.AcademicYear == period.AcademicYear && item.Term == period.Semester).ToList();
                var finalizedPeriodGrades = finalizedGrades.Where(item => item.StudentId == student.Id && item.AcademicYear == period.AcademicYear && item.Term == period.Semester).ToList();
                var confirmedPeriodGrades = finalizedPeriodGrades.Select(item => item.CourseId).Distinct().Count() == expectedCourseCount
                    ? finalizedPeriodGrades
                    : [];
                var cohortTimetableCourses = timetableAssignments.Where(item =>
                        item.AcademicYear == period.AcademicYear
                        && item.Semester == period.Semester
                        && item.YearLevel == resultYear
                        && item.ScheduleEntry?.Shift == resultShift
                        && StudentCurriculumPolicy.IncludesDepartment(resultYear, resultDepartmentId, item.Course?.DepartmentId))
                    .Select(item => item.Course!)
                    .ToList();
                var fallbackAssignedCourses = cohortTimetableCourses.Count > 0
                    ? []
                    : courseAssignments.Where(item =>
                        item.AcademicYear == period.AcademicYear
                        && item.Semester == period.Semester
                        && item.YearLevel == resultYear
                        && StudentCurriculumPolicy.IncludesDepartment(resultYear, resultDepartmentId, item.DepartmentId)
                        && item.Course is not null).Select(item => item.Course!).ToList();
                var fallbackCatalogCourses = cohortTimetableCourses.Count > 0 || fallbackAssignedCourses.Count > 0
                    ? []
                    : catalogCourses.Where(item => item.YearLevel == resultYear
                        && item.Semester == period.Semester
                        && StudentCurriculumPolicy.IncludesDepartment(resultYear, resultDepartmentId, item.DepartmentId)).ToList();
                var learningCourses = submittedGrades.Where(item => item.Course is not null).Select(item => item.Course!)
                    .Concat(cohortTimetableCourses)
                    .Concat(fallbackAssignedCourses)
                    .Concat(fallbackCatalogCourses)
                    .GroupBy(item => item.Id)
                    .Select(item => item.First())
                    .OrderBy(item => item.CourseCode)
                    .Take(expectedCourseCount)
                    .ToList();
                var periodGrades = learningCourses.Select(course =>
                {
                    var finalized = confirmedPeriodGrades.FirstOrDefault(item => item.CourseId == course.Id);
                    return new CourseResultDto(course.Id, course.CourseCode, course.Name, finalized?.Score, finalized?.LetterGrade ?? "Draft", finalized is not null);
                }).ToList();
                var approvedResults = periodGrades.Where(item => item.IsApproved).ToList();
                var total = approvedResults.Sum(item => item.Score ?? 0m);
                var average = SemesterResultRules.Average(approvedResults.Select(item => item.Score ?? 0m), expectedCourseCount);
                var statuses = timetableAttendance.Count > 0 ? timetableAttendance.Select(item => item.Status).ToList() : periodAttendance.Select(item => item.Status).ToList();
                var absent = statuses.Count(item => item == "Absent");
                var permission = statuses.Count(item => item is "Excused" or "Permission");
                var attendanceScore = attendanceRules.Score(weights.Attendance, absent, permission);
                var attendanceGrade = attendanceRules.Grade(attendanceScore, weights.Attendance);
                var overallGrade = thresholds.Letter(average);
                var totalGrade = SemesterResultRules.Outcome(approvedResults.Select(item => item.Grade).ToList(), average, thresholds, expectedCourseCount, attendanceRules.Outcome(absent, permission));
                var publicationStatus = publication is not null ? "Published" : approvedResults.Count == expectedCourseCount ? "Ready" : "Draft";
                results.Add(new SemesterResultDto(
                    student.Id, student.StudentCode,
                    enrollment is not null
                        ? codeFormat.PeriodLinkedWithConfiguredPrefix(enrollment.EnrollmentCode, enrollment.YearLevel, enrollment.Semester, "resultCodePrefix", "RES")
                        : codeFormat.LinkedWithConfiguredPrefix(student.StudentCode, "student", string.Empty, "resultCodePrefix", "RES"),
                    student.FullName, resultDepartmentId, resultDepartment, resultYear, resultShift,
                    period.AcademicYear, period.Semester,
                    statuses.Count(item => item is "Present" or "Late"), absent,
                    permission, attendanceScore, weights.Attendance, attendanceGrade,
                    periodGrades, expectedCourseCount, approvedResults.Count, total, average, overallGrade, totalGrade,
                    publicationStatus, publication is not null, publication?.PublishedAtUtc));
            }
        }
        var items = results.OrderBy(item => item.Year).ThenBy(item => ShiftOrder(item.Shift)).ThenBy(item => item.FullName).ThenByDescending(item => item.AcademicYear).ThenBy(item => item.Semester).ToList();
        return PagedResult<SemesterResultDto>.Create(items, page, totalCount);
    }

    public async Task<ResultPublicationReadinessDto> GetPublicationReadinessAsync(CancellationToken cancellationToken)
    {
        var settings = await db.SystemSettings.AsNoTracking()
            .Where(item => item.Section == "academic-year" || item.Section == "semester" || item.Section == "grade-rules")
            .ToListAsync(cancellationToken);
        var currentAcademicYear = settings.FirstOrDefault(item => item.Section == "academic-year" && item.Key == "currentYear")?.Value ?? "2026–2027";
        var currentSemester = settings.FirstOrDefault(item => item.Section == "semester" && item.Key == "currentTerm")?.Value ?? "Semester 1";
        var expectedCourseCount = ExpectedCourseCount(settings.Where(item => item.Section == "grade-rules").ToDictionary(item => item.Key, item => item.Value));
        var completed = db.GradeRecords.AsNoTracking()
            .Where(grade => grade.FinalizedAtUtc.HasValue && (grade.ReviewStatus == "Approved" || grade.SubmittedByTeacherId == null))
            .GroupBy(grade => new { grade.StudentId, grade.AcademicYear, grade.Term })
            .Where(group => group.Select(grade => grade.CourseId).Distinct().Count() == expectedCourseCount
                && (group.Key.AcademicYear == currentAcademicYear && group.Key.Term == currentSemester
                    || !db.SemesterResultPublications.Any(publication => publication.StudentId == group.Key.StudentId && publication.AcademicYear == group.Key.AcademicYear && publication.Term == group.Key.Term)
                    || !db.FinancialAccounts.Any(account => account.StudentId == group.Key.StudentId && account.AcademicYear == group.Key.AcademicYear && account.Semester == group.Key.Term && account.ClosedAtUtc.HasValue)))
            .Select(group => new { group.Key.StudentId, AcademicYear = group.Key.AcademicYear, Semester = group.Key.Term });
        var currentEnrollments = db.StudentEnrollments.AsNoTracking()
            .Where(enrollment => enrollment.Status == "Active" && enrollment.AcademicYear == currentAcademicYear && enrollment.Semester == currentSemester)
            .Select(enrollment => new { enrollment.StudentId, enrollment.AcademicYear, enrollment.Semester });
        var candidates = completed.Concat(currentEnrollments).Distinct()
            .Where(candidate => db.Students.Any(student => student.Id == candidate.StudentId && student.Status != "Inactive"));
        var total = await candidates.CountAsync(cancellationToken);
        var published = await candidates.CountAsync(candidate => db.SemesterResultPublications.Any(publication =>
            publication.StudentId == candidate.StudentId
            && publication.AcademicYear == candidate.AcademicYear
            && publication.Term == candidate.Semester), cancellationToken);
        var ready = await candidates.CountAsync(candidate =>
            !db.SemesterResultPublications.Any(publication => publication.StudentId == candidate.StudentId && publication.AcademicYear == candidate.AcademicYear && publication.Term == candidate.Semester)
            && db.GradeRecords.Where(grade => grade.StudentId == candidate.StudentId
                    && grade.AcademicYear == candidate.AcademicYear
                    && grade.Term == candidate.Semester
                    && grade.FinalizedAtUtc.HasValue
                    && (grade.ReviewStatus == "Approved" || grade.SubmittedByTeacherId == null))
                .Select(grade => grade.CourseId)
                .Distinct()
                .Count() == expectedCourseCount, cancellationToken);
        var draft = total - published - ready;
        return new(total, ready, draft, published, ready > 0 && draft == 0);
    }

    public async Task<int> PublishAllAsync(CancellationToken cancellationToken)
    {
        var current = await GetAllCurrentAsync(cancellationToken);
        var unpublished = current.Where(item => item.PublicationStatus != "Published").ToList();
        if (unpublished.Any(item => item.PublicationStatus != "Ready"))
            throw new InvalidOperationException("Every current student must have all course grades confirmed before Semester Results can be published together.");
        var ready = unpublished.Where(item => item.PublicationStatus == "Ready").ToList();
        if (ready.Count == 0) return 0;

        var studentIds = ready.Select(item => item.StudentId).ToList();
        var students = await db.Students.AsNoTracking()
            .Where(item => studentIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var publicationEnrollments = (await db.StudentEnrollments.AsNoTracking()
                .Where(item => studentIds.Contains(item.StudentId))
                .ToListAsync(cancellationToken))
            .ToDictionary(item => (item.StudentId, item.AcademicYear, item.Semester));
        foreach (var result in ready)
        {
            if (!publicationEnrollments.TryGetValue((result.StudentId, result.AcademicYear, result.Semester), out var enrollment))
                throw new InvalidOperationException("A Semester Result cannot be published without its Enrollment Semester.");
            var publication = new SemesterResultPublication
            {
                StudentEnrollmentId = enrollment.Id,
                StudentId = result.StudentId,
                AcademicYear = result.AcademicYear,
                Term = result.Semester,
                PublishedAtUtc = DateTime.UtcNow
            };
            db.SemesterResultPublications.Add(publication);
            db.AuditLogs.Add(new AuditLog
            {
                ResourceId = publication.Id,
                Type = "Academic result",
                Subject = students.GetValueOrDefault(result.StudentId)?.FullName ?? result.FullName,
                Action = "Published",
                Details = $"{result.ResultCode} · {result.AcademicYear} · {result.Semester} · published with all Student results · read-only · eligible for semester archive after payment closure and semester end"
            });
        }
        foreach (var result in ready)
            await ReleaseProgressionAsync(result.StudentId, result.AcademicYear, result.Semester, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ready.Count;
    }

    private async Task<List<SemesterResultDto>> GetAllCurrentAsync(CancellationToken cancellationToken)
    {
        var items = new List<SemesterResultDto>();
        var pageNumber = 1;
        while (true)
        {
            var page = await GetAsync(null, null, null, null, false, null, false, null, null, new PageRequest(pageNumber, PageRequest.MaximumPageSize), cancellationToken);
            items.AddRange(page.Items);
            if (pageNumber >= page.TotalPages) return items;
            pageNumber++;
        }
    }

    private async Task ReleaseProgressionAsync(Guid studentId, string academicYear, string term, CancellationToken cancellationToken)
    {
        var requirePaid = await db.SystemSettings.AsNoTracking()
            .Where(item => item.Section == "finance" && item.Key == "requirePaidForAdvancement")
            .Select(item => item.Value)
            .FirstOrDefaultAsync(cancellationToken);
        var paymentRequired = !bool.TryParse(requirePaid, out var configured) || configured;
        var account = await db.FinancialAccounts
            .Include(item => item.StudentEnrollment)
            .Include(item => item.Student)
            .FirstOrDefaultAsync(item => item.StudentId == studentId && item.AcademicYear == academicYear && item.Semester == term, cancellationToken);
        if (account is null || paymentRequired && !account.ClosedAtUtc.HasValue) return;
        await progression.ReleaseAsync(
            new StudentEnrollmentProgressionRequest(
                account.StudentId,
                account.StudentEnrollmentId,
                account.AcademicYear,
                account.Semester,
                account.FinancialAccountCode,
                account.Status),
            cancellationToken);
    }

    private sealed record Period(string AcademicYear, string Semester);
    private sealed class ResultPeriodCandidate
    {
        public Guid StudentId { get; init; }
        public string AcademicYear { get; init; } = string.Empty;
        public string Semester { get; init; } = string.Empty;
    }
    private sealed record SessionAttendance(Guid StudentId, string AcademicYear, string Semester, string Status);

    private static int ExpectedCourseCount(IReadOnlyDictionary<string, string> settings) =>
        int.TryParse(settings.GetValueOrDefault("expectedCourseCount"), out var count) && count > 0
            ? count
            : SemesterResultRules.ExpectedCourseCount;

    private static int ShiftOrder(string value) => value.Trim().ToLowerInvariant() switch
    {
        "morning" => 0,
        "afternoon" => 1,
        "evening" => 2,
        "weekend" => 3,
        _ => 4
    };
}
