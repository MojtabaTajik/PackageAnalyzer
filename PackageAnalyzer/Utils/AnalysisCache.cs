using System.Text.Json;
using PackageAnalyzer.Models;

namespace PackageAnalyzer.Utils;

public static class AnalysisCache
{ 
    public static bool TryGet(string projectName, out ProjectInfo? projectInfo)
    {
        var analysisResultPath = PathUtils.GetAnalysisResultPath();
        var analysisFilePath = PathUtils.GetAnalysisFilePath(analysisResultPath, projectName);
        
        if (!File.Exists(analysisFilePath))
        {
            projectInfo = null;
            return false;
        }
        
        var serializedPackages = File.ReadAllText(analysisFilePath);
        projectInfo = JsonSerializer.Deserialize<ProjectInfo>(serializedPackages);
        return true;
    }
    
    public static Task Store(string projectName, ProjectInfo projectInfo)
    {
        var analysisResultPath = PathUtils.GetAnalysisResultPath();
        if (!Directory.Exists(analysisResultPath))
        {
            Directory.CreateDirectory(analysisResultPath);
        }
    
        var analysisFilePath = PathUtils.GetAnalysisFilePath(analysisResultPath, projectName);
        var serializedPackages = JsonSerializer.Serialize(projectInfo, new JsonSerializerOptions { WriteIndented = true });
        return File.WriteAllTextAsync(analysisFilePath, serializedPackages);
    }
}