namespace InstituteManagement.Architecture.Tests;

public sealed class PersistenceConfigurationTests
{
    [Fact]
    public void Created_at_column_mapping_is_owned_by_infrastructure()
    {
        var entitySource = File.ReadAllText(Path.Combine(
            ArchitectureTestPaths.SourceDirectory("InstituteManagement.Domain"),
            "Common",
            "Entity.cs"));
        var contextSource = File.ReadAllText(Path.Combine(
            ArchitectureTestPaths.SourceDirectory("InstituteManagement.Infrastructure"),
            "Persistence",
            "InstituteDbContext.cs"));

        Assert.DoesNotContain("System.ComponentModel.DataAnnotations", entitySource, StringComparison.Ordinal);
        Assert.DoesNotContain("[Column(", entitySource, StringComparison.Ordinal);
        Assert.Contains("SetColumnName(\"CreatedAtUtc\")", contextSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Every_concrete_domain_entity_has_its_own_EF_configuration()
    {
        var entityDirectory = Path.Combine(
            ArchitectureTestPaths.SourceDirectory("InstituteManagement.Domain"),
            "Entities");
        var configurationDirectory = Path.Combine(
            ArchitectureTestPaths.SourceDirectory("InstituteManagement.Infrastructure"),
            "Persistence",
            "Configurations");

        var missingConfigurations = Directory
            .EnumerateFiles(entityDirectory, "*.cs", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(entityName => !HasDedicatedConfiguration(configurationDirectory, entityName))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            missingConfigurations.Length == 0,
            $"Concrete entities without a dedicated EF configuration: {string.Join(", ", missingConfigurations)}");
    }

    [Theory]
    [InlineData("Management/Students/StudentConfiguration.cs", "StudentCode")]
    [InlineData("Management/Teachers/TeacherConfiguration.cs", "TeacherCode")]
    [InlineData("Management/Courses/CourseConfiguration.cs", "CourseCode")]
    [InlineData("Management/Departments/DepartmentConfiguration.cs", "DepartmentCode")]
    [InlineData("Management/Classrooms/ClassroomConfiguration.cs", "ClassroomCode")]
    [InlineData("Grades/GradeRecordConfiguration.cs", "GradeCode")]
    [InlineData("History/AuditLogConfiguration.cs", "AuditLogCode")]
    public void Permanent_public_codes_have_database_unique_indexes(string relativePath, string property)
    {
        var source = ConfigurationSource(relativePath);

        Assert.Contains($"HasIndex(x => x.{property}).IsUnique()", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("StudentEnrollments/StudentEnrollmentConfiguration.cs", "StudentId")]
    [InlineData("TeacherAssignments/TeacherAssignmentConfiguration.cs", "TeacherId")]
    [InlineData("CourseAssignments/CourseAssignmentConfiguration.cs", "CourseId")]
    [InlineData("ClassroomAssignments/ClassroomAssignmentConfiguration.cs", "ClassroomId")]
    [InlineData("TimetableEnrollments/TimetableEnrollmentConfiguration.cs", "ScheduleEntryId")]
    public void Enrollment_ledgers_are_unique_and_indexed_per_academic_period(string relativePath, string masterKey)
    {
        var source = ConfigurationSource(Path.Combine("Enrollment", relativePath));

        Assert.Contains(
            $"new {{ x.{masterKey}, x.AcademicYear, x.Semester }}).IsUnique()",
            source,
            StringComparison.Ordinal);
        Assert.Contains(
            "new { x.EnrollmentCode, x.AcademicYear, x.Semester }).IsUnique()",
            source,
            StringComparison.Ordinal);
        Assert.Contains(
            "new { x.AcademicYear, x.Semester, x.Status",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Reporting_fact_tables_have_query_shaped_indexes()
    {
        var attendance = ConfigurationSource("Attendance/AttendanceRecordConfiguration.cs");
        var grades = ConfigurationSource("Grades/GradeRecordConfiguration.cs");
        var sessions = ConfigurationSource("Records/ClassSessionRecordConfiguration.cs");
        var audit = ConfigurationSource("History/AuditLogConfiguration.cs");

        Assert.Contains("HasIndex(x => x.Date)", attendance, StringComparison.Ordinal);
        Assert.Contains("new { x.AcademicYear, x.Term, x.CreateAt }", attendance, StringComparison.Ordinal);
        Assert.Contains("new { x.AcademicYear, x.Term }", grades, StringComparison.Ordinal);
        Assert.Contains("HasIndex(x => x.UpdatedAtUtc)", grades, StringComparison.Ordinal);
        Assert.Contains("HasIndex(x => x.SessionDate)", sessions, StringComparison.Ordinal);
        Assert.Contains("new { x.DepartmentId, x.YearLevel }", sessions, StringComparison.Ordinal);
        Assert.Contains("HasIndex(x => x.CreateAt)", audit, StringComparison.Ordinal);
        Assert.Contains("new { x.Type, x.Action, x.ResourceId }", audit, StringComparison.Ordinal);
    }

    private static bool HasDedicatedConfiguration(string configurationDirectory, string? entityName)
    {
        if (string.IsNullOrWhiteSpace(entityName))
        {
            return false;
        }

        return Directory.EnumerateFiles(configurationDirectory, $"{entityName}Configuration.cs", SearchOption.AllDirectories)
            .Any(path => File.ReadAllText(path).Contains(
                $"IEntityTypeConfiguration<{entityName}>",
                StringComparison.Ordinal));
    }

    private static string ConfigurationSource(string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return File.ReadAllText(Path.Combine(
            ArchitectureTestPaths.SourceDirectory("InstituteManagement.Infrastructure"),
            "Persistence",
            "Configurations",
            normalized));
    }
}
