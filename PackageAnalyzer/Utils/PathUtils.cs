using System.Reflection;

namespace PackageAnalyzer.Utils;

public static class PathUtils
{
    public static string GetAnalysisResultPath() => Path.Combine(GetAppPath(), "AnalysisResult");
    public static string GetAnalysisFilePath(string analysisPath, string projectName) => Path.Combine(analysisPath ,$"{projectName}_Analysis.json");
    private static string GetAppPath() => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException("Unable to determine application path.");
}