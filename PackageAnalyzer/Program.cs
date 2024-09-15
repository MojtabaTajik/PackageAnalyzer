using PackageAnalyzer.Models;
using PackageAnalyzer.Services;
using PackageAnalyzer.Utils;
using Spectre.Console;

try
{
    AnsiConsole.MarkupLine(InfoStyle("Welcome to Package Analyzer!"));
    AnsiConsole.MarkupLine(PromptStyle("Please provide the path to the solution you want to analyze:"));
    string? solutionPath = Console.ReadLine();

    if (!Directory.Exists(solutionPath))
    {
        AnsiConsole.MarkupLine(ErrorStyle("Provided path is invalid."));
        return;
    }

    var packageConfigFiles = SearchUtils.SearchForConfigFiles(solutionPath);
    if (!packageConfigFiles.Any())
    {
        AnsiConsole.MarkupLine(ErrorStyle("No package config files found in the provided path."));
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
    
    AnsiConsole.MarkupLine(InfoStyle("Select the report you want to generate:"));
    
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
                AnsiConsole.MarkupLine(ErrorStyle("Invalid choice, please try again."));
                break;
        }
    }
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine(ErrorStyle(ex.Message));
}

string? AskForReportChoice()
{
    AnsiConsole.MarkupLine($"{PromptStyle("1.")} Packages by project report");
    AnsiConsole.MarkupLine($"{PromptStyle("2.")} Project by packages report");
    AnsiConsole.MarkupLine($"{PromptStyle("3.")} Anomalies report");
    AnsiConsole.MarkupLine($"{ErrorStyle("X.")} Exit");
    return Console.ReadLine();
}

string PromptStyle(string text) => $"[bold orange1]{text}[/]";
string ErrorStyle(string message) => $"[bold red]{message}[/]";
string InfoStyle(string message) => $"[bold yellow2]{message}[/]";