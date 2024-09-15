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
        var dependencyInfo = await resource.GetDependencyInfoAsync(
            package.Id,
            package.Version,
            new SourceCacheContext(),
            NullLogger.Instance,
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
                var depPackageIdentity = new PackageIdentity(
                    dependency.Id,
                    dependency.VersionRange.MinVersion);

                // Create a PackageInfo for this dependency
                var depPackageInfo = new PackageInfo(
                    dependency.Id,
                    dependency.VersionRange.MinVersion.ToString(),
                    framework.GetShortFolderName());

                // Recursively get transitive dependencies for this dependency
                depPackageInfo = depPackageInfo with
                {
                    TransitiveDependencies = await GetTransitiveDependencies(
                        depPackageIdentity,
                        framework,
                        processedPackages)
                };

                // Add the depPackageInfo to the transitive dependencies list
                transitiveDeps.Add(depPackageInfo);
            }
        }

        return transitiveDeps;
    }
}