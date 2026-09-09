using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Enrollment;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Operations;

public sealed class OperationEnrollmentSourceService(
    InstituteDbContext db,
    OperationEnrollmentPeriodService periodService)
{
    public async Task<OperationEnrollmentSource> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var period = await periodService.GetAsync(cancellationToken);
        var students = await db.StudentEnrollments.AsNoTracking()
            .Include(enrollment => enrollment.Student)
            .Include(enrollment => enrollment.Department)
            .Where(enrollment =>
                enrollment.AcademicYear == period.AcademicYear
                && enrollment.Semester == period.Semester
                && enrollment.Status == "Active"
                && enrollment.Student != null
                && enrollment.Student.Status != "Inactive"
                && (!departmentId.HasValue || enrollment.DepartmentId == departmentId))
            .ToListAsync(cancellationToken);

        var studentCohorts = students.Select(StudentCohort).ToHashSet();
        var timetables = await db.TimetableEnrollments.AsNoTracking()
            .Include(enrollment => enrollment.ScheduleEntry)
            .Include(enrollment => enrollment.Course)
                .ThenInclude(course => course!.Department)
            .Include(enrollment => enrollment.Teacher)
            .Include(enrollment => enrollment.Classroom)
            .Where(enrollment =>
                enrollment.AcademicYear == period.AcademicYear
                && enrollment.Semester == period.Semester
                && enrollment.Status == "Active"
                && enrollment.ScheduleEntry != null
                && enrollment.ScheduleEntry.Status != "Cancelled"
                && enrollment.Course != null
                && enrollment.Teacher != null
                && enrollment.Classroom != null)
            .ToListAsync(cancellationToken);

        timetables = timetables.Where(enrollment =>
        {
            var cohort = TimetableCohort(enrollment);
            return cohort.HasValue
                && (!departmentId.HasValue || cohort.Value.DepartmentId == departmentId)
                && studentCohorts.Contains(cohort.Value);
        }).ToList();

        var timetableCohorts = timetables
            .Select(TimetableCohort)
            .Where(cohort => cohort.HasValue)
            .Select(cohort => cohort!.Value)
            .ToHashSet();
        students = students.Where(enrollment => timetableCohorts.Contains(StudentCohort(enrollment))).ToList();

        return new OperationEnrollmentSource(period, students, timetables);
    }

    internal static EnrollmentCohortKey StudentCohort(StudentEnrollment enrollment) =>
        EnrollmentCohortKey.Create(
            enrollment.DepartmentId,
            enrollment.YearLevel,
            enrollment.Shift,
            enrollment.AcademicYear,
            enrollment.Semester);

    internal static EnrollmentCohortKey? TimetableCohort(TimetableEnrollment enrollment)
    {
        var departmentId = enrollment.Course?.DepartmentId;
        return departmentId.HasValue && enrollment.ScheduleEntry is not null
            ? EnrollmentCohortKey.Create(
                departmentId.Value,
                enrollment.YearLevel,
                enrollment.ScheduleEntry.Shift,
                enrollment.AcademicYear,
                enrollment.Semester)
            : null;
    }
}

public sealed record OperationEnrollmentSource(
    OperationEnrollmentPeriod Period,
    IReadOnlyList<StudentEnrollment> Students,
    IReadOnlyList<TimetableEnrollment> Timetables);
