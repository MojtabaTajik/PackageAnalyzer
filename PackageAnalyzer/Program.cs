using PackageAnalyzer.Helpers;
using PackageAnalyzer.Models;
using PackageAnalyzer.Services;
using PackageAnalyzer.Utils;
using Spectre.Console;

try
{
    AnsiConsole.MarkupLine("Welcome to Package Analyzer!".InfoStyle());
    AnsiConsole.MarkupLine("Please provide the path to the solution you want to analyze:".PromptStyle());
    string? solutionPath = Console.ReadLine();

    if (!Directory.Exists(solutionPath))
    {
        AnsiConsole.MarkupLine("Provided path is invalid.".ErrorStyle());
        return;
    }

    var packageConfigFiles = SearchUtils.SearchForConfigFiles(solutionPath);
    if (!packageConfigFiles.Any())
    {
        AnsiConsole.MarkupLine("No package config files found in the provided path.".ErrorStyle());
        return;
    }
    var projectsInfo = await ProjectInfoUtils.GetProjectInfo(packageConfigFiles);

    foreach (var project in projectsInfo)
    {
        var cacheExists = AnalysisCache.TryGet(project.Name, out ProjectInfo? tempProject);
        if (!cacheExists)
        {
            AnsiConsole.MarkupLine($"Analyzing project {project.Name}...");
            await NugetService.FillTransitiveDependencies(project);
            await AnalysisCache.Store(project.Name, project);
        }
    }
    
    AnsiConsole.MarkupLine("Select the report you want to generate:".InfoStyle());
    
    var exitApp = false;
    while (exitApp == false)
    {
        var reportChoice = AskForReportChoice();
        switch (reportChoice)
        {
            case "1":
                ReportUtils.PrintPackagesByProjectReport(projectsInfo);
                break;
            case "2":
                ReportUtils.PrintProjectByPackagesReport(projectsInfo);
                break;
            case "3":
                ReportUtils.PrintAnomaliesReport(projectsInfo);
                break;
            case "x":
                exitApp = true;
                break;
            default:
                AnsiConsole.MarkupLine("Invalid choice, please try again.".ErrorStyle());
                break;
        }
    }
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine(ex.Message.ErrorStyle());
}

string? AskForReportChoice()
{
    AnsiConsole.MarkupLine($"{"1".PromptStyle()} Packages by project report");
    AnsiConsole.MarkupLine($"{"2".PromptStyle()}  Project by packages report");
    AnsiConsole.MarkupLine($"{"3".PromptStyle()} Anomalies report");
    AnsiConsole.MarkupLine($"{"X".PromptStyle()}  Exit");
    return Console.ReadLine();
}

