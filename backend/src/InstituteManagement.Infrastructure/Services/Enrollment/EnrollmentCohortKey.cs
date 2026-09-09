namespace InstituteManagement.Infrastructure.Services.Enrollment;

internal readonly record struct EnrollmentCohortKey(
    Guid DepartmentId,
    int YearLevel,
    string Shift,
    string AcademicYear,
    string Semester)
{
    public static EnrollmentCohortKey Create(
        Guid departmentId,
        int yearLevel,
        string shift,
        string academicYear,
        string semester) =>
        new(
            departmentId,
            yearLevel,
            shift.Trim().ToUpperInvariant(),
            academicYear.Trim().ToUpperInvariant(),
            semester.Trim().ToUpperInvariant());
}
