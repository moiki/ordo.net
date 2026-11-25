using System.Xml.Linq;
using Shouldly;

namespace Ordo.ArchitectureTests;

public sealed class ProjectReferenceMatrixTests
{
    private static readonly string[] ForbiddenApplicationPackages =
    [
        "Microsoft.EntityFrameworkCore",
        "RabbitMQ.Client",
        "Microsoft.AspNetCore",
        "MediatR",
        "MassTransit",
        "Wolverine",
        "NServiceBus"
    ];

    [Fact]
    public void Project_references_should_match_the_dependency_matrix()
    {
        string repositoryRoot = FindRepositoryRoot();
        string[] projectFiles = Directory
            .EnumerateFiles(Path.Combine(repositoryRoot, "src"), "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(repositoryRoot, "tests"), "*.csproj", SearchOption.AllDirectories))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        projectFiles.Length.ShouldBeGreaterThan(0);

        var violations = new List<string>();

        foreach (string projectFile in projectFiles)
        {
            string projectName = Path.GetFileNameWithoutExtension(projectFile);

            if (projectFile.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var document = XDocument.Load(projectFile);
            string[] projectReferences = document.Descendants("ProjectReference")
                .Select(element => (element.Attribute("Include")?.Value ?? string.Empty).Replace('\\', Path.DirectorySeparatorChar))
                .Select(path => Path.GetFileNameWithoutExtension(path))
                .ToArray();
            string[] packageReferences = document.Descendants("PackageReference")
                .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
                .ToArray();

            string? service = projectName.StartsWith("Ordo.Orders.", StringComparison.Ordinal)
                ? "Orders"
                : projectName.StartsWith("Ordo.Inventory.", StringComparison.Ordinal)
                    ? "Inventory"
                    : null;

            foreach (string reference in projectReferences)
            {
                bool crossesServiceBoundary = service == "Orders" && reference.StartsWith("Ordo.Inventory.", StringComparison.Ordinal)
                    || service == "Inventory" && reference.StartsWith("Ordo.Orders.", StringComparison.Ordinal);

                if (crossesServiceBoundary)
                {
                    violations.Add($"{projectName}: forbidden cross-service ProjectReference -> {reference}");
                }
            }

            if (projectName.EndsWith(".Domain", StringComparison.Ordinal)
                || projectName == "Ordo.Contracts")
            {
                foreach (string reference in projectReferences)
                {
                    violations.Add($"{projectName}: forbidden ProjectReference -> {reference}");
                }

                foreach (string package in packageReferences)
                {
                    violations.Add($"{projectName}: forbidden PackageReference -> {package}");
                }
            }
            else if (projectName == "Ordo.BuildingBlocks.Messaging.Abstractions")
            {
                // docs/architecture.md §4: Messaging.Abstractions may reference Ordo.Contracts only.
                foreach (string reference in projectReferences.Where(reference => reference != "Ordo.Contracts"))
                {
                    violations.Add($"{projectName}: ProjectReference outside Messaging.Abstractions allowlist -> {reference}");
                }

                foreach (string package in packageReferences)
                {
                    violations.Add($"{projectName}: forbidden PackageReference -> {package}");
                }
            }
            else if (projectName.EndsWith(".Application", StringComparison.Ordinal) && service is not null)
            {
                string[] allowed =
                [
                    $"Ordo.{service}.Domain",
                    "Ordo.Contracts",
                    "Ordo.BuildingBlocks.Messaging.Abstractions"
                ];

                foreach (string reference in projectReferences.Where(reference => !allowed.Contains(reference, StringComparer.Ordinal)))
                {
                    violations.Add($"{projectName}: ProjectReference outside Application allowlist -> {reference}");
                }

                foreach (string package in packageReferences.Where(package => ForbiddenApplicationPackages.Contains(package, StringComparer.Ordinal)))
                {
                    violations.Add($"{projectName}: forbidden PackageReference -> {package}");
                }
            }
            else if (projectName.StartsWith("Ordo.BuildingBlocks.", StringComparison.Ordinal))
            {
                foreach (string reference in projectReferences.Where(reference => service is null && (reference.Contains("Orders", StringComparison.Ordinal) || reference.Contains("Inventory", StringComparison.Ordinal))))
                {
                    violations.Add($"{projectName}: building block references a service -> {reference}");
                }
            }
        }

        violations.ShouldBeEmpty();
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
