namespace InstituteManagement.Architecture.Tests;

public sealed class FeatureFolderStructureTests
{
    private static readonly string[] EnrollmentResources =
    [
        "Students",
        "Teachers",
        "Courses",
        "Classrooms",
        "Departments",
        "Timetable"
    ];

    [Fact]
    public void Controllers_are_grouped_below_technical_role_folder()
    {
        var controllerRoot = Path.Combine(
            ArchitectureTestPaths.SourceDirectory("InstituteManagement.API"),
            "Controllers");

        var flatControllers = Directory.EnumerateFiles(controllerRoot, "*Controller.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .ToArray();

        Assert.True(flatControllers.Length == 0, $"Controllers must be grouped by feature: {string.Join(", ", flatControllers)}");
    }

    [Fact]
    public void Domain_entities_are_grouped_by_resource()
    {
        var entityRoot = Path.Combine(
            ArchitectureTestPaths.SourceDirectory("InstituteManagement.Domain"),
            "Entities");

        var flatEntities = Directory.EnumerateFiles(entityRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .ToArray();

        Assert.True(flatEntities.Length == 0, $"Entities must be grouped by resource: {string.Join(", ", flatEntities)}");
    }

    [Fact]
    public void Enrollment_has_resource_specific_application_and_controller_folders()
    {
        var applicationRoot = Path.Combine(
            ArchitectureTestPaths.SourceDirectory("InstituteManagement.Application"),
            "Features",
            "Enrollment");
        var controllerRoot = Path.Combine(
            ArchitectureTestPaths.SourceDirectory("InstituteManagement.API"),
            "Controllers",
            "Enrollment");

        foreach (var resource in EnrollmentResources)
        {
            Assert.True(Directory.Exists(Path.Combine(applicationRoot, resource)), $"Missing Enrollment application folder: {resource}");
            Assert.True(Directory.Exists(Path.Combine(controllerRoot, resource)), $"Missing Enrollment controller folder: {resource}");
        }

        Assert.DoesNotContain(
            Directory.EnumerateFiles(applicationRoot, "*.cs", SearchOption.TopDirectoryOnly),
            path => Path.GetFileName(path).Contains("Resource", StringComparison.Ordinal));
        Assert.Empty(Directory.EnumerateFiles(controllerRoot, "*Controller.cs", SearchOption.TopDirectoryOnly));
    }
}
