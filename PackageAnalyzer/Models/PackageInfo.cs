namespace PackageAnalyzer.Models;

public record PackageInfo(string Name, string Version, string TargetFramework)
{
    public List<PackageInfo>? TransitiveDependencies { get; set; }
}
