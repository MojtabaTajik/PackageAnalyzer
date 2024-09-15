using System.Text.Json;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using PackageAnalyzer.Models;
using PackageAnalyzer.Services;
using PackageAnalyzer.Utils;
using Spectre.Console;

try
{
    Console.WriteLine("Enter solution path:");
    string? solutionPath = Console.ReadLine();

    if (!Directory.Exists(solutionPath))
    {
        Console.WriteLine("Solution path does not exist.");
        return;
    }

    var packageConfigFiles = PackageConfigSearcher.GetPackageConfigFiles(solutionPath);
    if (!packageConfigFiles.Any())
    {
        Console.WriteLine("No package.config files found.");
        return;
    }
    var projectsInfo = await ProjectInfoUtils.GetProjectInfos(packageConfigFiles);

    foreach (var project in projectsInfo)
    {
        var tempProject = project;
        var cacheExists = AnalysisCache.CacheExists(tempProject.Name);
        if (cacheExists)
        {
            Console.WriteLine($"Analysis for project {tempProject.Name} already exists. Loading...");
            var cachedAnalysis = await AnalysisCache.GetAnalysis(tempProject.Name);
            tempProject = JsonSerializer.Deserialize<ProjectInfo>(cachedAnalysis);
        }
        else
        {
            Console.WriteLine($"Processing project: {tempProject.Name}");

            foreach (var package in tempProject.Packages)
            {
                var packageIdentity = new PackageIdentity(package.Name, NuGetVersion.Parse(package.Version));
                var framework = NuGetFramework.ParseFolder(package.TargetFramework);

                var processedPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Assign transitive dependencies directly
                package.TransitiveDependencies = await NugetService.GetTransitiveDependencies(
                    packageIdentity,
                    framework,
                    processedPackages);
            }

            await AnalysisCache.StoreAnalysis(tempProject.Name, tempProject);
        }

        var packageTree = BuildPackageTree(tempProject);
        AnsiConsole.Write(packageTree);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"An error occurred: {ex.Message}");
}
Console.WriteLine("Press any key to exit...");
Console.ReadKey();

static Tree BuildPackageTree(ProjectInfo projectInfo)
{
    var root = new Tree($"{projectInfo.Name} [grey]({projectInfo.Framework})[/]");

    foreach (var packageNode in projectInfo.Packages.Select(BuildPackageNode))
    {
        root.AddNode(packageNode);
    }

    return root;
}

static TreeNode BuildPackageNode(PackageInfo package)
{
    var nodeContent = new Markup($"{package.Name} [grey]({package.Version})[/]");
    var node = new TreeNode(nodeContent);

    if (package.TransitiveDependencies != null && package.TransitiveDependencies.Any())
    {
        foreach (var dep in package.TransitiveDependencies)
        {
            var childNode = BuildPackageNode(dep);
            node.AddNode(childNode);
        }
    }

    return node;
}
