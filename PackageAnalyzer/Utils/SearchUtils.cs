using PackageAnalyzer.Models;

namespace PackageAnalyzer.Utils;

public static class SearchUtils
{
    public static List<PackageConfigInfo> SearchForConfigFiles(string solutionPath)
    {
        if (!Directory.Exists(solutionPath))
        {
            throw new DirectoryNotFoundException("Solution path does not exist.");
        }

        var configFiles = Directory.GetFiles(solutionPath, "packages.config", SearchOption.AllDirectories);

        var packageConfigInfos = new List<PackageConfigInfo>();
        foreach (var configFile in configFiles)
        {
            var projectName = Directory.GetParent(configFile)?.Name ?? "Unknown";
            packageConfigInfos.Add(new PackageConfigInfo(projectName, configFile));
        }
        
        return packageConfigInfos;
    }
}