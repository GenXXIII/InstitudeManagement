namespace InstituteManagement.Architecture.Tests;

public sealed class Phase3ApiArchitectureTests
{
    [Theory]
    [InlineData("Finance/FinanceController.cs", "IFinanceService")]
    [InlineData("Grades/Submission/GradesController.cs", "IGradeService")]
    [InlineData("Results/ResultsController.cs", "IResultQueryService")]
    [InlineData("Attendance/ClassSessions/ClassSessionStartsController.cs", "IClassSessionStartService")]
    [InlineData("Attendance/ClassSessions/ClassPermissionRequestsController.cs", "IClassPermissionService")]
    public void Core_workflow_controllers_dispatch_one_use_case_through_mediator(
        string relativePath,
        string forbiddenService)
    {
        var source = File.ReadAllText(Path.Combine(
            ArchitectureTestPaths.SourceDirectory("InstituteManagement.API"),
            "Controllers",
            relativePath.Replace('/', Path.DirectorySeparatorChar)));

        Assert.Contains("ISender sender", source, StringComparison.Ordinal);
        Assert.Contains("sender.Send", source, StringComparison.Ordinal);
        Assert.DoesNotContain(forbiddenService, source, StringComparison.Ordinal);
    }

    [Fact]
    public void Validation_pipeline_discovers_request_validators_from_the_application_assembly()
    {
        var source = File.ReadAllText(Path.Combine(
            ArchitectureTestPaths.SourceDirectory("InstituteManagement.Application"),
            "DependencyInjection.cs"));

        Assert.Contains("IPipelineBehavior<,>", source, StringComparison.Ordinal);
        Assert.Contains("IRequestValidator<>", source, StringComparison.Ordinal);
        Assert.Contains("assembly.DefinedTypes", source, StringComparison.Ordinal);
    }
}
