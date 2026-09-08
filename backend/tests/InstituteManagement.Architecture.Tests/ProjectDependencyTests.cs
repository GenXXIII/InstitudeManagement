using System.Xml.Linq;

namespace InstituteManagement.Architecture.Tests;

public sealed class ProjectDependencyTests
{
    [Theory]
    [InlineData("InstituteManagement.Domain")]
    [InlineData("InstituteManagement.Application", "MediatR")]
    [InlineData("InstituteManagement.Infrastructure", "Microsoft.EntityFrameworkCore.Design", "Microsoft.EntityFrameworkCore.InMemory", "Microsoft.EntityFrameworkCore.SqlServer", "StackExchange.Redis")]
    [InlineData("InstituteManagement.API", "Microsoft.AspNetCore.OpenApi", "Swashbuckle.AspNetCore")]
    public void Production_projects_use_only_layer_appropriate_packages(
        string projectName,
        params string[] expectedPackages)
    {
        var projectPath = ArchitectureTestPaths.SourceProject(projectName);
        var document = XDocument.Load(projectPath);
        var actualPackages = document
            .Descendants()
            .Where(element => element.Name.LocalName == "PackageReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedPackages.Order(StringComparer.Ordinal), actualPackages);
    }

    [Theory]
    [InlineData("InstituteManagement.Domain")]
    [InlineData("InstituteManagement.Application", "InstituteManagement.Domain")]
    [InlineData("InstituteManagement.Infrastructure", "InstituteManagement.Application", "InstituteManagement.Domain")]
    [InlineData("InstituteManagement.API", "InstituteManagement.Application", "InstituteManagement.Infrastructure")]
    public void Project_references_follow_clean_architecture_direction(
        string projectName,
        params string[] expectedReferences)
    {
        var projectPath = ArchitectureTestPaths.SourceProject(projectName);
        var document = XDocument.Load(projectPath);
        var actualReferences = document
            .Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => Path.GetFileNameWithoutExtension(include!.Replace('\\', '/')))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedReferences.Order(StringComparer.Ordinal), actualReferences);
    }
}
