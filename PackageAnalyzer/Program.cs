using System.Reflection;
using System.Text.Json;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using PackageAnalyzer.Models;
using PackageAnalyzer.Services;
using Spectre.Console;

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

var packageConfigFiles = Directory.GetFiles(solutionPath, "*packages.config", SearchOption.AllDirectories);

var projectsWithPackages = new Dictionary<string, List<PackageInfo>>();
foreach (var packageFile in packageConfigFiles)
{
    var projectName = Path.GetFileNameWithoutExtension(packageFile).Replace("_packages", "");
    var packageFileContent = await File.ReadAllTextAsync(packageFile);
    var packages = PackageConfigParser.GetPackages(packageFileContent);
    
    projectsWithPackages[projectName] = packages;
}

foreach (var project in projectsWithPackages)
{
    string projectName = project.Key;
    List<PackageInfo> packages = project.Value;
    
    var analysisExists = File.Exists(GetAnalysisFilePath(GetAnalysisResultPath(), project.Key));
    if (analysisExists)
    {
        Console.WriteLine($"Analysis for project {project.Key} already exists. Loading...");
        var analysisFilePath = GetAnalysisFilePath(GetAnalysisResultPath(), project.Key);
        var analysisFileContent = await File.ReadAllTextAsync(analysisFilePath);

        packages = JsonSerializer.Deserialize<List<PackageInfo>>(analysisFileContent);
    }
    else
    {
        Console.WriteLine($"Processing project: {projectName}");

        foreach (var package in packages)
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
        
        await StoreAnalysis(projectName, packages);
    }

    var packageTree = BuildPackageTree(packages);
    AnsiConsole.Write(packageTree);

    Console.WriteLine($"Finished processing project: {projectName}");
}

static Task StoreAnalysis(string projectName, List<PackageInfo> packages)
{
    var analysisResultPath = GetAnalysisResultPath();
    if (!Directory.Exists(analysisResultPath))
    {
        Directory.CreateDirectory(analysisResultPath);
    }
    
    var analysisFilePath = GetAnalysisFilePath(analysisResultPath, projectName);
    var serializedPackages = JsonSerializer.Serialize(packages, new JsonSerializerOptions { WriteIndented = true });
    return File.WriteAllTextAsync(analysisFilePath, serializedPackages);
}

static string GetAnalysisResultPath() => Path.Combine(GetAppPath(), "AnalysisResult");
static string GetAnalysisFilePath(string analysisPath, string projectName) => Path.Combine(analysisPath ,$"{projectName}_Analysis.json");
static string GetAppPath() => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException("Unable to determine application path.");

static Tree BuildPackageTree(List<PackageInfo> packages)
{
    var root = new Tree("Packages");

    foreach (var package in packages)
    {
        var packageNode = BuildPackageNode(package);
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
