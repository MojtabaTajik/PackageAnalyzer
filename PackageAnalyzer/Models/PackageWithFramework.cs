using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace PackageAnalyzer.Models;

public record PackageWithFramework
{
    public PackageIdentity Identity { get; }
    public NuGetFramework Framework { get; }

    public PackageWithFramework(string name, string version, string framework)
    {
        if (string.IsNullOrEmpty(framework))
        {
            framework = "4.8";
        }
        Identity = new PackageIdentity(name, NuGetVersion.Parse(version));
        Framework = NuGetFramework.ParseFolder(framework);
    }
}