namespace InstituteManagement.Architecture.Tests;

public sealed class LayerBoundaryTests
{
    [Fact]
    public void Domain_has_no_outer_layer_or_framework_dependencies()
    {
        AssertNoForbiddenDependencies(
            "InstituteManagement.Domain",
            "InstituteManagement.Application",
            "InstituteManagement.Infrastructure",
            "InstituteManagement.API",
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.Extensions",
            "MediatR",
            "StackExchange.Redis",
            "System.ComponentModel.DataAnnotations",
            "System.Data.SqlClient",
            "System.Text.Json");
    }

    [Fact]
    public void Application_has_no_web_or_infrastructure_dependencies()
    {
        AssertNoForbiddenDependencies(
            "InstituteManagement.Application",
            "InstituteManagement.Infrastructure",
            "InstituteManagement.API",
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore",
            "StackExchange.Redis",
            "System.Data.SqlClient");
    }

    [Fact]
    public void Api_has_no_direct_persistence_or_cache_dependencies()
    {
        AssertNoForbiddenDependencies(
            "InstituteManagement.API",
            "InstituteManagement.Infrastructure.Persistence",
            "InstituteManagement.Infrastructure.Services",
            "Microsoft.EntityFrameworkCore",
            "StackExchange.Redis",
            "System.Data.SqlClient",
            "InstituteDbContext");
    }

    [Fact]
    public void Infrastructure_has_no_web_dependency()
    {
        AssertNoForbiddenDependencies(
            "InstituteManagement.Infrastructure",
            "InstituteManagement.API");
    }

    private static void AssertNoForbiddenDependencies(string projectName, params string[] forbiddenTokens)
    {
        var violations = ArchitectureTestPaths.CSharpFiles(projectName)
            .SelectMany(path => FindViolations(path, forbiddenTokens))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"{projectName} contains forbidden dependencies:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    private static IEnumerable<string> FindViolations(string path, IReadOnlyCollection<string> forbiddenTokens)
    {
        var lineNumber = 0;

        foreach (var line in File.ReadLines(path))
        {
            lineNumber++;

            foreach (var token in forbiddenTokens)
            {
                if (line.Contains(token, StringComparison.Ordinal))
                {
                    yield return $"{Path.GetRelativePath(ArchitectureTestPaths.BackendRoot, path)}:{lineNumber} contains {token}";
                }
            }
        }
    }
}
