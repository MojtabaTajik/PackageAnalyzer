using PackageAnalyzer.Models;
using Spectre.Console;

namespace PackageAnalyzer.Utils;

public static class ReportUtils
{
    public static Tree BuildPackageTree(ProjectInfo projectInfo)
    {
        var root = new Tree($"{projectInfo.Name} [grey]({projectInfo.Framework})[/]");

        foreach (var packageNode in projectInfo.Packages.Select(BuildPackageNode))
        {
            root.AddNode(packageNode);
        }

        return root;
    }

    private static TreeNode BuildPackageNode(PackageInfo package)
    {
        var nodeContent = new Markup($"{package.Name} [grey]({package.Version})[/]");
        var node = new TreeNode(nodeContent);

        if (package.TransitiveDependencies != null && package.TransitiveDependencies.Any())
        {
            foreach (var dep in package.TransitiveDependencies)
            {
                var childNode = BuildPackageNode(dep);
                node.AddNode(childNode);
            }
        }

        return node;
    }
}