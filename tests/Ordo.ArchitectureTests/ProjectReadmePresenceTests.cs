using Shouldly;

namespace Ordo.ArchitectureTests;

public sealed class ProjectReadmePresenceTests
{
    [Fact]
    public void Every_project_directory_should_contain_a_readme()
    {
        string repositoryRoot = FindRepositoryRoot();
        string[] projectDirectories = new[] { "src", "tests" }
            .SelectMany(topLevel => Directory.EnumerateDirectories(Path.Combine(repositoryRoot, topLevel), "*", SearchOption.AllDirectories))
            .Where(directory => Directory.EnumerateFiles(directory, "*.csproj").Any())
            .OrderBy(directory => directory, StringComparer.Ordinal)
            .ToArray();

        projectDirectories.Length.ShouldBeGreaterThan(0);

        string[] missing = projectDirectories
            .Where(directory => !File.Exists(Path.Combine(directory, "README.md")))
            .Select(directory => Path.GetRelativePath(repositoryRoot, directory))
            .ToArray();

        missing.ShouldBeEmpty();
    }

    private static string FindRepositoryRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Ordo.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Could not locate Ordo.slnx from the test output directory.");
    }
}
