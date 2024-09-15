﻿using PackageAnalyzer.Models;
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

            if (project.Packages != null)
            {
                foreach (var package in project.Packages)
                {
                    var packageNode = projectNode.AddNode($"[green]{package.Name}[/] [yellow]{package.Version}[/]");

                    if (package.TransitiveDependencies != null)
                    {
                        foreach (var transitiveDependency in package.TransitiveDependencies)
                        {
                            packageNode.AddNode(
                                $"[green]{transitiveDependency.Name}[/] [yellow]{transitiveDependency.Version}[/]");
                        }
                    }
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
            if (project.Packages != null)
            {
                foreach (var package in project.Packages)
                {
                    if (!packageGroups.ContainsKey(package.Name))
                    {
                        packageGroups[package.Name] = new List<(string, string)>();
                    }

                    packageGroups[package.Name].Add((project.Name, package.Version));
                }
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
            if (project.Packages != null)
            {
                foreach (var package in project.Packages)
                {
                    if (!packageGroups.ContainsKey(package.Name))
                    {
                        packageGroups[package.Name] = new List<(string, string)>();
                    }

                    packageGroups[package.Name].Add((project.Name, package.Version));
                }
            }
        }

        // List to hold anomalies
        var anomalies = new List<string>();

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
                    anomalies.Add(
                        $"[yellow]{projectInfo.ProjectName}[/] upgrade [green]{packageGroup.Key}[/] to [red]{mostCommonVersion}[/] to fix anomaly.");
                }
            }
        }

        // Print anomalies
        if (anomalies.Count > 0)
        {
            AnsiConsole.MarkupLine("[bold red]Anomalies Found[/]");
            foreach (var anomaly in anomalies)
            {
                AnsiConsole.MarkupLine($"[white]{anomaly}[/]");
            }
        }
        else
        {
            AnsiConsole.MarkupLine("[bold green]No anomalies found![/]");
        }
    }
}