using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace PackageAnalyzer.Services;

public class NugetService
{
    public static async Task GetTransitiveDependencies(
        PackageIdentity package,
        NuGetFramework framework,
        HashSet<string> processedPackages,
        Dictionary<string, HashSet<string>> transitiveDependencies)
    {
        if (!processedPackages.Add(package.Id))
            return;

        var repo = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
        var resource = await repo.GetResourceAsync<FindPackageByIdResource>();
        var dependencyInfo = await resource.GetDependencyInfoAsync(package.Id, package.Version, new SourceCacheContext(), NullLogger.Instance, CancellationToken.None);

        if (dependencyInfo == null)
            return;

        foreach (var dependencyGroup in dependencyInfo.DependencyGroups)
        {
            if (!dependencyGroup.TargetFramework.Equals(NuGetFramework.AnyFramework) &&
                !framework.Equals(dependencyGroup.TargetFramework))
            {
                continue;
            }
            
            foreach (var dependency in dependencyGroup.Packages)
            {
                if (!transitiveDependencies.ContainsKey(dependency.Id))
                    transitiveDependencies[dependency.Id] = new HashSet<string>();
                transitiveDependencies[dependency.Id].Add(package.Id);

                var depPackage = new PackageIdentity(dependency.Id, dependency.VersionRange.MinVersion);
                await GetTransitiveDependencies(depPackage, framework, processedPackages, transitiveDependencies);
            }
        }
    }
}