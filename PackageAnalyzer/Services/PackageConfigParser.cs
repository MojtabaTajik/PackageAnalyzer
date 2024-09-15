using System.Xml.Linq;
using PackageAnalyzer.Models;

namespace PackageAnalyzer.Services;

public class PackageConfigParser
{
    public static List<PackageInfo> GetPackages(string configContent)
    {
        if (string.IsNullOrEmpty(configContent))
        {
            return [];
        }
        
        XDocument doc = XDocument.Parse(configContent);

        var packages = doc.Descendants("package")
            .Select(package => new PackageInfo(
                GetAttributeValue(package, "id"),
                GetAttributeValue(package, "version"),
                GetAttributeValue(package, "targetFramework")))
            .ToList();

        return packages;
    }
    
    private static string GetAttributeValue(XElement element, string attributeName)
    {
        return element.Attribute(attributeName)?.Value ?? string.Empty;
    }
}