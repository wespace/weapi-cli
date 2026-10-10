using System.Text.RegularExpressions;

namespace WeApi.Cli.Services;

/// <summary>
/// Preflight analysis engine that evaluates solution structure, enforces complexity guards,
/// and computes atomic in-memory file changes for framework migration.
/// </summary>
public static partial class MigrationAnalyzer
{
    /// <summary>Matches singular &lt;TargetFramework&gt; elements in MSBuild project files.</summary>
    [GeneratedRegex(@"<TargetFramework>\s*([^<\s]+)\s*</TargetFramework>", RegexOptions.IgnoreCase)]
    private static partial Regex TargetFrameworkRegex();

    /// <summary>Matches multi-targeting &lt;TargetFrameworks&gt; elements in MSBuild project files.</summary>
    [GeneratedRegex(@"<TargetFrameworks>\s*([^<\s]+)\s*</TargetFrameworks>", RegexOptions.IgnoreCase)]
    private static partial Regex TargetFrameworksPluralRegex();

    /// <summary>Matches &lt;PackageReference&gt; XML tags.</summary>
    [GeneratedRegex(@"<PackageReference\s+[^>]*?>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex PackageReferenceElementRegex();

    /// <summary>Extracts the 'Include' attribute value from a PackageReference element.</summary>
    [GeneratedRegex(@"\bInclude\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase)]
    private static partial Regex IncludeAttrRegex();

    /// <summary>Extracts the 'Version' attribute value from a PackageReference element.</summary>
    [GeneratedRegex(@"\bVersion\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase)]
    private static partial Regex VersionAttrRegex();

    /// <summary>Matches the SDK "version" field in global.json files.</summary>
    [GeneratedRegex(@"""version""\s*:\s*""[^""]+""", RegexOptions.IgnoreCase)]
    private static partial Regex GlobalJsonVersionRegex();

    /// <summary>
    /// Performs preflight analysis on a discovered solution to validate compatibility and build a <see cref="MigrationPlan"/>.
    /// </summary>
    public static async Task<MigrationPlan> AnalyzeAsync(
        DiscoveryResult discovery,
        string? gitRoot,
        string targetFramework,
        bool updateGlobalJson,
        CancellationToken cancellationToken = default)
    {
        var projectName = Path.GetFileName(discovery.SolutionRoot);
        var warnings = new List<string>();
        var errors = new List<string>();
        var fileChanges = new List<FileMigrationChange>();
        var packageChanges = new List<PackageChange>();

        // 1. Complexity Guard: Project Discovery
        if (discovery.ProjectFiles.Count == 0 && discovery.DirectoryBuildPropsPath is null)
        {
            return CreateUnsupportedPlan(
                discovery,
                gitRoot,
                projectName,
                targetFramework,
                "No .NET projects or Directory.Build.props found in solution root.");
        }

        // 2. Complexity Guards:
        // a) Legacy non-SDK projects, multi-targeting, and custom TFM suffixes
        var detectedFrameworks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var uniqueProjects = discovery.ProjectFiles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        foreach (var projPath in uniqueProjects)
        {
            var content = await File.ReadAllTextAsync(projPath, cancellationToken);

            // Legacy non-SDK check
            if (content.Contains("<TargetFrameworkVersion>", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("<ProductVersion>", StringComparison.OrdinalIgnoreCase) ||
                !content.Contains("Sdk=", StringComparison.OrdinalIgnoreCase))
            {
                return CreateUnsupportedPlan(
                    discovery,
                    gitRoot,
                    projectName,
                    targetFramework,
                    $"Legacy non-SDK project detected in '{Path.GetFileName(projPath)}'. Automatic migration is only supported for SDK-style projects.");
            }

            // Multi-targeting check
            if (TargetFrameworksPluralRegex().IsMatch(content))
            {
                return CreateUnsupportedPlan(
                    discovery,
                    gitRoot,
                    projectName,
                    targetFramework,
                    $"Multi-targeting project detected in '{Path.GetFileName(projPath)}' (<TargetFrameworks>). Multi-targeting projects require manual migration.");
            }

            var tfmMatches = TargetFrameworkRegex().Matches(content);
            if (tfmMatches.Count > 0)
            {
                foreach (Match tfmMatch in tfmMatches)
                {
                    var tfm = tfmMatch.Groups[1].Value.Trim();
                    if (tfm.Contains('-'))
                    {
                        return CreateUnsupportedPlan(
                            discovery,
                            gitRoot,
                            projectName,
                            targetFramework,
                            $"Custom or OS-specific target framework '{tfm}' detected in '{Path.GetFileName(projPath)}'. Automatic migration is not supported.");
                    }
                    detectedFrameworks.Add(tfm);
                }
            }
            else if (discovery.DirectoryBuildPropsPath is null)
            {
                // Missing TargetFramework without Directory.Build.props (imported/custom MSBuild framework definitions)
                return CreateUnsupportedPlan(
                    discovery,
                    gitRoot,
                    projectName,
                    targetFramework,
                    $"Project '{Path.GetFileName(projPath)}' does not define <TargetFramework> directly or via Directory.Build.props. Custom or imported MSBuild framework definitions require manual migration.");
            }
        }

        // b) Ambiguous project structure: multiple projects with conflicting TFMs and no Directory.Build.props
        if (discovery.DirectoryBuildPropsPath is null && detectedFrameworks.Count > 1)
        {
            return CreateUnsupportedPlan(
                discovery,
                gitRoot,
                projectName,
                targetFramework,
                $"Ambiguous project structure: projects target different frameworks ({string.Join(", ", detectedFrameworks)}). Multi-framework solutions require manual migration.");
        }

        // 3. Determine Project Type
        var projectType = ProjectType.GenericSdkProject;
        string currentFramework = "net10.0";

        if (discovery.DirectoryBuildPropsPath is not null)
        {
            var buildPropsContent = await File.ReadAllTextAsync(discovery.DirectoryBuildPropsPath, cancellationToken);
            if (buildPropsContent.Contains("<WeApiVersion>", StringComparison.OrdinalIgnoreCase))
            {
                projectType = ProjectType.WeApiSolution;
            }

            var match = TargetFrameworkRegex().Match(buildPropsContent);
            if (match.Success)
            {
                currentFramework = match.Groups[1].Value.Trim();
            }
        }
        else if (discovery.ProjectFiles.Count > 0)
        {
            var firstProj = await File.ReadAllTextAsync(discovery.ProjectFiles[0], cancellationToken);
            var match = TargetFrameworkRegex().Match(firstProj);
            if (match.Success)
            {
                currentFramework = match.Groups[1].Value.Trim();
            }
        }

        // 4. Check "Already Targeting"
        if (string.Equals(currentFramework, targetFramework, StringComparison.OrdinalIgnoreCase))
        {
            return new MigrationPlan(
                SolutionRoot: discovery.SolutionRoot,
                GitRoot: gitRoot,
                ProjectName: projectName,
                ProjectType: projectType,
                CurrentFramework: currentFramework,
                TargetFramework: targetFramework,
                CurrentSdkVersion: null,
                SelectedSdkVersion: null,
                RequiresSdkInstallation: false,
                UpdateGlobalJson: updateGlobalJson,
                ProjectFiles: discovery.ProjectFiles,
                FileChanges: Array.Empty<FileMigrationChange>(),
                PackageChanges: Array.Empty<PackageChange>(),
                Warnings: Array.Empty<string>(),
                Errors: Array.Empty<string>(),
                IsSupported: true,
                IsAlreadyTargeting: true);
        }

        // 5. SDK Resolution
        var sdkResult = await SdkResolver.ResolveAsync(discovery.SolutionRoot, targetFramework, cancellationToken);

        if (sdkResult.GlobalJsonBlocksTarget && !updateGlobalJson)
        {
            errors.Add(
                $"global.json is pinned to SDK '{sdkResult.GlobalJsonSdkVersion}', which cannot build '{targetFramework}'. " +
                "Re-run with --update-global-json or update global.json manually.");
        }

        var requiresSdkInstallation = !sdkResult.CanBuildTarget;
        if (requiresSdkInstallation)
        {
            warnings.Add(
                $"No installed .NET SDK capable of building '{targetFramework}' was found on this system. " +
                "Run with '--install-sdk' to install it automatically.");
        }

        // 6. Plan File Changes
        if (projectType == ProjectType.WeApiSolution && discovery.DirectoryBuildPropsPath is not null)
        {
            // WeApi Single Source of Truth migration
            var propsOriginal = await File.ReadAllTextAsync(discovery.DirectoryBuildPropsPath, cancellationToken);
            var propsUpdated = TargetFrameworkRegex().Replace(
                propsOriginal,
                $"<TargetFramework>{targetFramework}</TargetFramework>");

            fileChanges.Add(new FileMigrationChange(
                FilePath: discovery.DirectoryBuildPropsPath,
                ExistedBefore: true,
                OriginalContent: propsOriginal,
                UpdatedContent: propsUpdated,
                Description: $"Update TargetFramework from '{currentFramework}' to '{targetFramework}' in Directory.Build.props"));

            // Calculate WeApi dynamic package shifts for display
            var currentPackages = PackageCompatibility.GetCompatiblePackages(currentFramework);
            var targetPackages = PackageCompatibility.GetCompatiblePackages(targetFramework);

            foreach (var (pkgId, targetVer) in targetPackages)
            {
                var curVer = currentPackages.TryGetValue(pkgId, out var v) ? v : "dynamic";
                if (curVer != targetVer)
                {
                    packageChanges.Add(new PackageChange(pkgId, curVer, targetVer));
                }
            }
        }
        else
        {
            // Generic SDK-style project migration
            foreach (var projPath in discovery.ProjectFiles)
            {
                var original = await File.ReadAllTextAsync(projPath, cancellationToken);
                var updated = original;

                // Update TargetFramework
                if (TargetFrameworkRegex().IsMatch(updated))
                {
                    updated = TargetFrameworkRegex().Replace(
                        updated,
                        $"<TargetFramework>{targetFramework}</TargetFramework>");
                }

                // Update only whitelisted packages
                var matches = PackageReferenceElementRegex().Matches(original);
                foreach (Match m in matches)
                {
                    var includeMatch = IncludeAttrRegex().Match(m.Value);
                    var versionMatch = VersionAttrRegex().Match(m.Value);

                    if (!includeMatch.Success || !versionMatch.Success)
                    {
                        continue;
                    }

                    var pkgId = includeMatch.Groups[1].Value;
                    var curVer = versionMatch.Groups[1].Value;

                    if (PackageCompatibility.IsKnownPackage(pkgId))
                    {
                        var targetVer = PackageCompatibility.GetTargetVersion(pkgId, targetFramework);
                        if (targetVer is not null && targetVer != curVer)
                        {
                            var updatedElement = m.Value.Replace(versionMatch.Value, $"Version=\"{targetVer}\"");
                            updated = updated.Replace(m.Value, updatedElement);
                            packageChanges.Add(new PackageChange(pkgId, curVer, targetVer));
                        }
                    }
                    else
                    {
                        warnings.Add($"Package '{pkgId}' ({curVer}) is not in WeApi's whitelist and was not modified. Please verify compatibility manually.");
                    }
                }

                if (original != updated)
                {
                    fileChanges.Add(new FileMigrationChange(
                        FilePath: projPath,
                        ExistedBefore: true,
                        OriginalContent: original,
                        UpdatedContent: updated,
                        Description: $"Update TargetFramework and compatible packages in {Path.GetFileName(projPath)}"));
                }
            }
        }

        // Handle global.json update if explicitly requested
        if (updateGlobalJson && discovery.GlobalJsonPath is not null && File.Exists(discovery.GlobalJsonPath))
        {
            var globalJsonOriginal = await File.ReadAllTextAsync(discovery.GlobalJsonPath, cancellationToken);
            var bestSdk = sdkResult.BestCompatibleInstalledSdk;

            if (bestSdk is not null)
            {
                var globalJsonUpdated = GlobalJsonVersionRegex().Replace(globalJsonOriginal, $@"""version"": ""{bestSdk}""");

                fileChanges.Add(new FileMigrationChange(
                    FilePath: discovery.GlobalJsonPath,
                    ExistedBefore: true,
                    OriginalContent: globalJsonOriginal,
                    UpdatedContent: globalJsonUpdated,
                    Description: $"Update global.json SDK version to installed SDK '{bestSdk}'"));
            }
        }

        var isSupported = errors.Count == 0;

        return new MigrationPlan(
            SolutionRoot: discovery.SolutionRoot,
            GitRoot: gitRoot,
            ProjectName: projectName,
            ProjectType: projectType,
            CurrentFramework: currentFramework,
            TargetFramework: targetFramework,
            CurrentSdkVersion: sdkResult.ActiveSdkVersion,
            SelectedSdkVersion: sdkResult.BestCompatibleInstalledSdk ?? sdkResult.ActiveSdkVersion,
            RequiresSdkInstallation: requiresSdkInstallation,
            UpdateGlobalJson: updateGlobalJson,
            ProjectFiles: discovery.ProjectFiles,
            FileChanges: fileChanges,
            PackageChanges: packageChanges,
            Warnings: warnings.Distinct().ToList(),
            Errors: errors,
            IsSupported: isSupported,
            IsAlreadyTargeting: false);
    }

    private static MigrationPlan CreateUnsupportedPlan(
        DiscoveryResult discovery,
        string? gitRoot,
        string projectName,
        string targetFramework,
        string reason)
    {
        return new MigrationPlan(
            SolutionRoot: discovery.SolutionRoot,
            GitRoot: gitRoot,
            ProjectName: projectName,
            ProjectType: ProjectType.Unsupported,
            CurrentFramework: "unknown",
            TargetFramework: targetFramework,
            CurrentSdkVersion: null,
            SelectedSdkVersion: null,
            RequiresSdkInstallation: false,
            UpdateGlobalJson: false,
            ProjectFiles: discovery.ProjectFiles,
            FileChanges: Array.Empty<FileMigrationChange>(),
            PackageChanges: Array.Empty<PackageChange>(),
            Warnings: Array.Empty<string>(),
            Errors: [reason],
            IsSupported: false,
            IsAlreadyTargeting: false);
    }
}
