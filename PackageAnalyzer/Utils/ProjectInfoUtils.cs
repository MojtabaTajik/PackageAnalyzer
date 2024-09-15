using PackageAnalyzer.Models;
using PackageAnalyzer.Services;

namespace PackageAnalyzer.Utils;

public static class ProjectInfoUtils
{
    public static async Task<List<ProjectInfo>> GetProjectInfo(List<PackageConfigInfo> packageConfigFiles)
    {
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

        return projectsInfo;
    }
}