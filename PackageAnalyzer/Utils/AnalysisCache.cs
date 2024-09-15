using System.Text.Json;
using PackageAnalyzer.Models;

namespace PackageAnalyzer.Utils;

public static class AnalysisCache
{
    public static bool CacheExists(string projectName)
    {
        var analysisResultPath = PathUtils.GetAnalysisResultPath();
        var analysisFilePath = PathUtils.GetAnalysisFilePath(analysisResultPath, projectName);
        return File.Exists(analysisFilePath);
    }
    
    public static Task StoreAnalysis(string projectName, ProjectInfo projectInfo)
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
    
    public static Task<string> GetAnalysis(string projectName)
    {
        var analysisResultPath = PathUtils.GetAnalysisResultPath();
        var analysisFilePath = PathUtils.GetAnalysisFilePath(analysisResultPath, projectName);
        
        return !File.Exists(analysisFilePath) ? Task.FromResult(string.Empty) : File.ReadAllTextAsync(analysisFilePath);
    }
}