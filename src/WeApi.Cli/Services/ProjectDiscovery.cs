namespace WeApi.Cli.Services;

public record DiscoveryResult(
    string SolutionRoot,
    string? SolutionFile,
    string? DirectoryBuildPropsPath,
    string? DirectoryPackagesPropsPath,
    string? GlobalJsonPath,
    IReadOnlyList<string> ProjectFiles);

public static class ProjectDiscovery
{
    public static DiscoveryResult? Discover(string? startDirectory, string? gitRoot)
    {
        if (string.IsNullOrWhiteSpace(startDirectory))
        {
            startDirectory = Directory.GetCurrentDirectory();
        }

        if (File.Exists(startDirectory))
        {
            startDirectory = Path.GetDirectoryName(startDirectory) ?? startDirectory;
        }

        var current = Path.GetFullPath(startDirectory);
        var normalizedGitRoot = !string.IsNullOrWhiteSpace(gitRoot) ? Path.GetFullPath(gitRoot) : null;

        // 1. Search up the directory tree for a Solution (*.slnx, *.sln) or Directory.Build.props
        while (!string.IsNullOrEmpty(current))
        {
            var slnxFiles = Directory.GetFiles(current, "*.slnx");
            var slnFiles = Directory.GetFiles(current, "*.sln");
            var buildProps = Path.Combine(current, "Directory.Build.props");

            if (slnxFiles.Length > 0 || slnFiles.Length > 0 || File.Exists(buildProps))
            {
                var solutionFile = slnxFiles.FirstOrDefault() ?? slnFiles.FirstOrDefault();
                var buildPropsPath = File.Exists(buildProps) ? buildProps : null;
                var packagesProps = Path.Combine(current, "Directory.Packages.props");
                var packagesPropsPath = File.Exists(packagesProps) ? packagesProps : null;
                var globalJson = Path.Combine(current, "global.json");
                var globalJsonPath = File.Exists(globalJson) ? globalJson : null;

                // Discover all .csproj files inside this solution root (excluding bin/obj)
                var projectFiles = Directory.GetFiles(current, "*.csproj", SearchOption.AllDirectories)
                    .Where(p => !IsInBinOrObjDirectory(p))
                    .ToList();

                return new DiscoveryResult(
                    SolutionRoot: current,
                    SolutionFile: solutionFile,
                    DirectoryBuildPropsPath: buildPropsPath,
                    DirectoryPackagesPropsPath: packagesPropsPath,
                    GlobalJsonPath: globalJsonPath,
                    ProjectFiles: projectFiles);
            }

            // If we have reached the Git root, do not search further up into parent repositories
            if (normalizedGitRoot is not null && string.Equals(current, normalizedGitRoot, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            var parent = Directory.GetParent(current);
            if (parent is null)
            {
                break;
            }

            current = parent.FullName;
        }

        // 2. If no solution or Directory.Build.props found, check if startDirectory itself (or its children) has .csproj files
        var startFull = Path.GetFullPath(startDirectory);
        if (Directory.Exists(startFull))
        {
            var directProjects = Directory.GetFiles(startFull, "*.csproj", SearchOption.AllDirectories)
                .Where(p => !IsInBinOrObjDirectory(p))
                .ToList();

            if (directProjects.Count > 0)
            {
                var packagesProps = Path.Combine(startFull, "Directory.Packages.props");
                var packagesPropsPath = File.Exists(packagesProps) ? packagesProps : null;
                var globalJson = Path.Combine(startFull, "global.json");
                var globalJsonPath = File.Exists(globalJson) ? globalJson : null;

                return new DiscoveryResult(
                    SolutionRoot: startFull,
                    SolutionFile: null,
                    DirectoryBuildPropsPath: null,
                    DirectoryPackagesPropsPath: packagesPropsPath,
                    GlobalJsonPath: globalJsonPath,
                    ProjectFiles: directProjects);
            }
        }

        return null;
    }

    private static bool IsInBinOrObjDirectory(string path)
    {
        return path.Contains("/bin/", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("\\bin\\", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/obj/", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("\\obj\\", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("bin/", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("bin\\", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("obj/", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("obj\\", StringComparison.OrdinalIgnoreCase);
    }
}
