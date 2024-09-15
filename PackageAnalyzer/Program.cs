using NuGet.Versioning;
using PackageAnalyzer.Models;
using PackageAnalyzer.Services;

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

var packageConfigFiles = Directory.GetFiles(solutionPath, "packages.config", SearchOption.AllDirectories);

var projectsWithPackages = new Dictionary<string, List<PackageInfo>>();
foreach (var packageFile in packageConfigFiles)
{
    var projectName = Path.GetFileNameWithoutExtension(packageFile).Replace("_packages", "");
    var packageFileContent = File.ReadAllText(packageFile);
    var packages = PackageConfigParser.GetPackages(packageFileContent);
    
    projectsWithPackages[projectName] = packages;
}

var allPackages = new Dictionary<string, HashSet<NuGetVersion>>();
var transitiveDependencies = new Dictionary<string, HashSet<string>>();
foreach (var project in projectsWithPackages)
{
    string projectName = project.Key;
    List<PackageInfo> packages = project.Value;
    
    Console.WriteLine($"Processing project: {projectName}");
    var directPackages = packages.Select(p => new PackageWithFramework(p.Name, p.Version, p.TargetFramework)).ToList();
    var processedPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var package in directPackages)
    {
        if (!allPackages.ContainsKey(package.Identity.Id))
            allPackages[package.Identity.Id] = new HashSet<NuGetVersion>();
        allPackages[package.Identity.Id].Add(package.Identity.Version);

        await NugetService.GetTransitiveDependencies(package.Identity, package.Framework, processedPackages, transitiveDependencies);
    }
    Console.WriteLine($"Finished processing project: {projectName}");
}