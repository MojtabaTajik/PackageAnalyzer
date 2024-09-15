using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using PackageAnalyzer.Models;

namespace PackageAnalyzer.Services;

public static class NugetService
{
    public static async Task<List<PackageInfo>> GetTransitiveDependencies(
        PackageIdentity package,
        NuGetFramework framework,
        HashSet<string> processedPackages)
    {
        var transitiveDeps = new List<PackageInfo>();

        if (!processedPackages.Add(package.Id))
            return transitiveDeps;

        var repo = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
        var resource = await repo.GetResourceAsync<FindPackageByIdResource>();
        var cacheContext = new SourceCacheContext();
        var logger = NullLogger.Instance;
        
        // Now that we have a valid version, get the dependency info
        var dependencyInfo = await resource.GetDependencyInfoAsync(
            package.Id,
            package.Version,
            cacheContext,
            logger,
            CancellationToken.None);

        if (dependencyInfo == null)
            return transitiveDeps;

        foreach (var dependencyGroup in dependencyInfo.DependencyGroups)
        {
            if (!dependencyGroup.TargetFramework.Equals(NuGetFramework.AnyFramework) &&
                !framework.Equals(dependencyGroup.TargetFramework))
            {
                continue;
            }

            foreach (var dependency in dependencyGroup.Packages)
            {
                var allVersions = await resource.GetAllVersionsAsync(
                    dependency.Id,
                    cacheContext,
                    logger,
                    CancellationToken.None);

                var bestVersion = dependency.VersionRange.FindBestMatch(allVersions);

                if (bestVersion == null)
                {
                    Console.WriteLine(
                        $"Could not find a matching version for dependency {dependency.Id} with version range {dependency.VersionRange?.ToNormalizedString() ?? "Unknown"}");
                    continue;
                }

                var depPackageIdentity = new PackageIdentity(dependency.Id, bestVersion);

                var depPackageInfo = new PackageInfo(
                    dependency.Id,
                    bestVersion.ToString(),
                    framework.GetShortFolderName())
                {
                    TransitiveDependencies = await GetTransitiveDependencies(
                        depPackageIdentity,
                        framework,
                        processedPackages)
                };

                transitiveDeps.Add(depPackageInfo);
            }
        }

        return transitiveDeps;
    }
}