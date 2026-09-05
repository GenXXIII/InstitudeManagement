namespace InstituteManagement.Architecture.Tests;

internal static class ArchitectureTestPaths
{
    public static string BackendRoot { get; } = FindBackendRoot();

    public static string SourceProject(string projectName) =>
        Path.Combine(BackendRoot, "src", projectName, $"{projectName}.csproj");

    public static string SourceDirectory(string projectName) =>
        Path.Combine(BackendRoot, "src", projectName);

    public static IEnumerable<string> CSharpFiles(string projectName) =>
        Directory.EnumerateFiles(SourceDirectory(projectName), "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(path));

    private static string FindBackendRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "InstituteManagement.slnx")) &&
                Directory.Exists(Path.Combine(directory.FullName, "src")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the backend root containing InstituteManagement.slnx and src.");
    }

    private static bool IsBuildOutput(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
        path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
}
