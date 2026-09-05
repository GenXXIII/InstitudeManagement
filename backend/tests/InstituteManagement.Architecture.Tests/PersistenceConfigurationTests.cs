namespace InstituteManagement.Architecture.Tests;

public sealed class PersistenceConfigurationTests
{
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
}
