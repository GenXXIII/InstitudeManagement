namespace InstituteManagement.Domain.Policies;

public static class StudentCurriculumPolicy
{
    public const int GeneralYearLevel = 1;

    public static bool IsGeneralYear(int yearLevel) => yearLevel == GeneralYearLevel;

    public static bool IncludesDepartment(int yearLevel, Guid studentDepartmentId, Guid? courseDepartmentId) =>
        IsGeneralYear(yearLevel) || courseDepartmentId == studentDepartmentId;
}
