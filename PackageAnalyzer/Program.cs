using PackageAnalyzer.Models;
using PackageAnalyzer.Services;
using PackageAnalyzer.Utils;

try
{
    Console.WriteLine("Enter solution path:");
    string? solutionPath = Console.ReadLine();

    if (!Directory.Exists(solutionPath))
    {
        Console.WriteLine("Solution path does not exist.");
        return;
    }

    var packageConfigFiles = SearchUtils.SearchForConfigFiles(solutionPath);
    if (!packageConfigFiles.Any())
    {
        Console.WriteLine("No package.config files found.");
        return;
    }
    var projectsInfo = await ProjectInfoUtils.GetProjectInfo(packageConfigFiles);

    foreach (var project in projectsInfo)
    {
        var cacheExists = AnalysisCache.TryGet(project.Name, out ProjectInfo? tempProject);
        if (!cacheExists)
        {
            Console.WriteLine($"Processing project: {project.Name}");
            await NugetService.FillTransitiveDependencies(project);
            await AnalysisCache.Store(project.Name, project);
        }
    }
    
    ReportUtils.PrintPackagesByProjectReport(projectsInfo);
    ReportUtils.PrintProjectByPackagesReport(projectsInfo);
    ReportUtils.PrintAnomaliesReport(projectsInfo);
}
catch (Exception ex)
{
    Console.WriteLine($"An error occurred: {ex.Message}");
}
Console.WriteLine("Press any key to exit...");
Console.ReadKey();

