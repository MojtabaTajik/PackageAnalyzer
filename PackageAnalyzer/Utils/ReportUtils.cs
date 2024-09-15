using PackageAnalyzer.Models;
using Spectre.Console;

namespace PackageAnalyzer.Utils;

public static class ReportUtils
{
    public static void PrintPackagesByProjectReport(List<ProjectInfo> projects)
    {
        var root = new Tree("[bold red]Packages by Project[/]");

        foreach (var project in projects)
        {
            var projectNode = root.AddNode($"[blue]{project.Name}[/] ({project.Framework})");

            if (project.Packages == null) continue;
            foreach (var package in project.Packages)
            {
                var packageNode = projectNode.AddNode($"[green]{package.Name}[/] [yellow]{package.Version}[/]");

                if (package.TransitiveDependencies == null) continue;
                foreach (var transitiveDependency in package.TransitiveDependencies)
                {
                    packageNode.AddNode(
                        $"[green]{transitiveDependency.Name}[/] [yellow]{transitiveDependency.Version}[/]");
                }
            }
        }

        AnsiConsole.Write(root);
    }

    public static void PrintProjectByPackagesReport(List<ProjectInfo> projects)
    {
        // Dictionary to group packages by name across all projects
        var packageGroups = new Dictionary<string, List<(string ProjectName, string Version)>>();

        // Iterate through all projects and their packages
        foreach (var project in projects)
        {
            if (project.Packages == null) continue;
            foreach (var package in project.Packages)
            {
                if (!packageGroups.ContainsKey(package.Name))
                {
                    packageGroups[package.Name] = new List<(string, string)>();
                }

                packageGroups[package.Name].Add((project.Name, package.Version));
            }
        }

        // Create root node for the report
        var root = new Tree("[bold red]Projects by Package[/]");
        
        // Loop through each package group and add to the tree
        foreach (var packageGroup in packageGroups)
        {
            var packageNode = root.AddNode($"[green]{packageGroup.Key}[/]"); // Package name

            // Get the most common version for the current package
            var mostCommonVersion = packageGroup.Value
                .GroupBy(x => x.Version)
                .OrderByDescending(g => g.Count())
                .First()
                .Key;

            foreach (var projectInfo in packageGroup.Value)
            {
                // Check if the version is the same as the most common version
                var versionColor = projectInfo.Version == mostCommonVersion ? "yellow" : "red";
                packageNode.AddNode($"[blue]{projectInfo.ProjectName}[/] -> [{versionColor}]{projectInfo.Version}[/]");
            }
        }

        // Print the tree
        AnsiConsole.Write(root);
    }

public static void PrintAnomaliesReport(List<ProjectInfo> projects)
{
    var packageGroups = new Dictionary<string, List<(string ProjectName, string Version)>>();

    // Iterate through all projects and their packages to group them by package name
    foreach (var project in projects)
    {
        if (project.Packages == null) continue;
        foreach (var package in project.Packages)
        {
            if (!packageGroups.ContainsKey(package.Name))
            {
                packageGroups[package.Name] = new List<(string, string)>();
            }

            packageGroups[package.Name].Add((project.Name, package.Version));
        }
    }

    // Dictionary to hold anomalies grouped by project
    var projectAnomalies = new Dictionary<string, List<string>>();

    foreach (var packageGroup in packageGroups)
    {
        var mostCommonVersion = packageGroup.Value
            .GroupBy(x => x.Version)
            .OrderByDescending(g => g.Count())
            .First()
            .Key;

        foreach (var projectInfo in packageGroup.Value)
        {
            // Check if the version is an anomaly (not the most common version)
            if (projectInfo.Version != mostCommonVersion)
            {
                var anomaly = $"upgrade [green]{packageGroup.Key}[/] to [red]{mostCommonVersion}[/] to fix anomaly.";
                
                // Group anomaly under the project name
                if (!projectAnomalies.ContainsKey(projectInfo.ProjectName))
                {
                    projectAnomalies[projectInfo.ProjectName] = new List<string>();
                }

                projectAnomalies[projectInfo.ProjectName].Add(anomaly);
            }
        }
    }

    // Print anomalies grouped by project
    if (projectAnomalies.Count > 0)
    {
        AnsiConsole.MarkupLine("[bold red]Anomalies Found[/]");
        foreach (var project in projectAnomalies)
        {
            AnsiConsole.MarkupLine($"[yellow]Project: {project.Key}[/]");
            foreach (var anomaly in project.Value)
            {
                AnsiConsole.MarkupLine($"  - {anomaly}");
            }
        }
    }
    else
    {
        AnsiConsole.MarkupLine("[bold green]No anomalies found![/]");
    }
}
}