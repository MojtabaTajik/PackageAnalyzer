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

    if (string.IsNullOrEmpty(solutionPath))
    {
        Console.WriteLine("Solution path is required.");
        return;
    }

    if (!Directory.Exists(solutionPath))
    {
        Console.WriteLine("Solution path does not exist.");
        return;
    }

    var packageConfigFiles = PackageConfigSearcher.GetPackageConfigFiles(solutionPath);

    var projectsInfo = new List<ProjectInfo>();
    foreach (var packageFile in packageConfigFiles)
    {
        var packageFileContent = await File.ReadAllTextAsync(packageFile.PackageConfigPath);
        var packages = PackageConfigParser.GetPackages(packageFileContent);
        var framework = packages.FirstOrDefault(f => f.TargetFramework != null)?.TargetFramework;
        
        var projectInfo = new ProjectInfo(packageFile.ProjectName, framework ?? "Unknown")
        {
            Packages = packages
        };
        projectsInfo.Add(projectInfo);
    }

    foreach (var project in projectsInfo)
    {
        var tempProject = project;
        var analysisExists = File.Exists(PathUtils.GetAnalysisFilePath(PathUtils.GetAnalysisResultPath(), tempProject.Name));
        if (analysisExists)
        {
            Console.WriteLine($"Analysis for project {tempProject.Name} already exists. Loading...");
            var analysisFilePath = PathUtils.GetAnalysisFilePath(PathUtils.GetAnalysisResultPath(), tempProject.Name);
            var analysisFileContent = await File.ReadAllTextAsync(analysisFilePath);

            tempProject = JsonSerializer.Deserialize<ProjectInfo>(analysisFileContent);
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

            await StoreAnalysis(tempProject.Name, tempProject);
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

static Task StoreAnalysis(string projectName, ProjectInfo packages)
{
    var analysisResultPath = PathUtils.GetAnalysisResultPath();
    if (!Directory.Exists(analysisResultPath))
    {
        Directory.CreateDirectory(analysisResultPath);
    }
    
    var analysisFilePath = PathUtils.GetAnalysisFilePath(analysisResultPath, projectName);
    var serializedPackages = JsonSerializer.Serialize(packages, new JsonSerializerOptions { WriteIndented = true });
    return File.WriteAllTextAsync(analysisFilePath, serializedPackages);
}

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
