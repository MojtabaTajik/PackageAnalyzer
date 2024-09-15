namespace PackageAnalyzer.Models;

public record ProjectInfo(string Name, string Framework)
{
    public List<PackageInfo>? Packages { get; set; }
}