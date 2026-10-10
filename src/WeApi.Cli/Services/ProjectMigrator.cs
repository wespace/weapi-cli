using WeApi.Cli.Common;

namespace WeApi.Cli.Services;

public record MigrationOptions(
    string? TargetPath,
    string TargetFramework,
    bool DryRun = false,
    bool ShowDiff = false,
    bool AutoConfirm = false,
    bool InstallSdk = false,
    bool UpdateGlobalJson = false);

public static class ProjectMigrator
{
    public static async Task<int> MigrateAsync(
        MigrationOptions options,
        CancellationToken cancellationToken = default)
    {
        ConsoleUi.WriteBanner();

        // 1. Framework Normalization
        if (!FrameworkNormalizer.TryNormalize(options.TargetFramework, out var targetFramework))
        {
            ConsoleUi.WriteError(
                $"Unsupported target framework '{options.TargetFramework}'. Supported frameworks are: {string.Join(", ", FrameworkNormalizer.GetSupportedFrameworks())}.");
            return 1;
        }

        // 2. Discover SolutionRoot and GitRoot
        var startDir = !string.IsNullOrWhiteSpace(options.TargetPath)
            ? Path.GetFullPath(options.TargetPath)
            : Directory.GetCurrentDirectory();

        if (!Directory.Exists(startDir) && File.Exists(startDir))
        {
            startDir = Path.GetDirectoryName(startDir)!;
        }

        if (!Directory.Exists(startDir))
        {
            ConsoleUi.WriteError($"The specified target directory '{startDir}' does not exist.");
            return 1;
        }

        var gitStatus = await GitHelper.CheckStatusAsync(startDir, cancellationToken);
        var discovery = ProjectDiscovery.Discover(startDir, gitStatus.GitRoot);

        if (discovery is null)
        {
            ConsoleUi.WriteError(
                $"Could not find a valid .NET project or solution (*.slnx, *.sln, *.csproj, or Directory.Build.props) in '{startDir}' or its parent directories.");
            return 1;
        }

        // 3. Git Working Tree Safety Check (if inside a Git repository)
        if (gitStatus.IsGitRepository && gitStatus.HasUncommittedChanges && !options.DryRun)
        {
            ConsoleUi.WriteWarning("You have uncommitted changes in this repository:");
            foreach (var file in gitStatus.ChangedFiles.Take(5))
            {
                Console.WriteLine($"    {file}");
            }
            if (gitStatus.ChangedFiles.Count > 5)
            {
                Console.WriteLine($"    ... and {gitStatus.ChangedFiles.Count - 5} more files.");
            }

            if (!options.AutoConfirm)
            {
                Console.Write("\nWe recommend committing your work first. Do you want to proceed anyway? (y/N): ");
                var response = Console.ReadLine()?.Trim().ToLowerInvariant();
                if (response != "y" && response != "yes")
                {
                    ConsoleUi.WriteInfo("Migration aborted safely. No files were modified.");
                    return 0;
                }
            }
        }

        // 4. Preflight Analysis & In-Memory MigrationPlan Generation
        ConsoleUi.WriteInfo($"Analyzing solution at '{discovery.SolutionRoot}'...");
        var plan = await MigrationAnalyzer.AnalyzeAsync(
            discovery,
            gitStatus.GitRoot,
            targetFramework,
            options.UpdateGlobalJson,
            cancellationToken);

        // 5. Check "Already Targeting"
        if (plan.IsAlreadyTargeting)
        {
            ConsoleUi.WriteSuccess(
                $"Project '{plan.ProjectName}' already targets '{targetFramework}'. No migration required.");
            return 0;
        }

        // 6. Complexity Guard / Unsupported Check
        if (!plan.IsSupported)
        {
            ConsoleUi.WriteError("Migration cannot be performed automatically:");
            foreach (var err in plan.Errors)
            {
                Console.WriteLine($"  • {err}");
            }
            ConsoleUi.WriteInfo("No files were modified.");
            return 1;
        }

        // 7. Dry-Run and Diff Preview
        if (options.DryRun || options.ShowDiff)
        {
            RenderDryRun(plan, options.ShowDiff);
            return 0;
        }

        // 8. SDK Resolution Check
        if (plan.RequiresSdkInstallation)
        {
            if (options.InstallSdk)
            {
                var installed = await SdkInstaller.InstallSdkAsync(targetFramework, cancellationToken);
                if (!installed)
                {
                    ConsoleUi.WriteError("Failed to install required .NET SDK. Migration aborted.");
                    return 1;
                }
            }
            else
            {
                ConsoleUi.WriteError(
                    $"A .NET SDK capable of building '{targetFramework}' is not installed.\n" +
                    $"Run with '--install-sdk' to install it automatically, or install it manually from https://dot.net.");
                return 1;
            }
        }

        // 9. Interactive Confirmation (unless --yes provided)
        if (!options.AutoConfirm)
        {
            Console.WriteLine();
            Console.WriteLine($"Ready to migrate '{plan.ProjectName}' from {plan.CurrentFramework} to {targetFramework}.");
            Console.WriteLine($"Files to modify: {plan.FileChanges.Count}");
            Console.Write("Proceed with migration? (Y/n): ");
            var confirm = Console.ReadLine()?.Trim().ToLowerInvariant();
            if (confirm == "n" || confirm == "no")
            {
                ConsoleUi.WriteInfo("Migration aborted by user. No files were modified.");
                return 0;
            }
        }

        // 10. Execution with Fail-Safe Rollback
        Console.WriteLine();
        ConsoleUi.WriteInfo("Applying migration changes...");

        var appliedChanges = new List<FileMigrationChange>();
        try
        {
            // Apply File Changes
            foreach (var change in plan.FileChanges)
            {
                await File.WriteAllTextAsync(change.FilePath, change.UpdatedContent, cancellationToken);
                appliedChanges.Add(change);
                ConsoleUi.WriteStep(change.Description);
            }

            // 11. Verification: Restore
            ConsoleUi.WriteInfo("Verifying package restore...");
            var restoreResult = await ProcessRunner.RunAsync(
                "dotnet",
                "restore",
                plan.SolutionRoot,
                cancellationToken);

            if (!restoreResult.Success)
            {
                ConsoleUi.WriteError($"Package restore failed:\n{restoreResult.StandardError}");
                await RollbackManager.ExecuteRollbackAsync(plan.FileChanges, cancellationToken);
                return 1;
            }
            ConsoleUi.WriteStep("Package restore verified successfully");

            // 12. Verification: Build
            ConsoleUi.WriteInfo("Verifying solution build...");
            var buildResult = await ProcessRunner.RunAsync(
                "dotnet",
                "build --no-restore",
                plan.SolutionRoot,
                cancellationToken);

            if (!buildResult.Success)
            {
                ConsoleUi.WriteError($"Solution build failed:\n{buildResult.StandardError}");
                await RollbackManager.ExecuteRollbackAsync(plan.FileChanges, cancellationToken);
                return 1;
            }
            ConsoleUi.WriteStep("Solution build verified successfully (0 errors)");
        }
        catch (Exception ex)
        {
            ConsoleUi.WriteError($"Migration failed with error: {ex.Message}");
            await RollbackManager.ExecuteRollbackAsync(appliedChanges.Count > 0 ? appliedChanges : plan.FileChanges, cancellationToken);
            return 1;
        }

        // 13. Rich Migration Summary
        RenderSuccessSummary(plan);

        return 0;
    }

    private static void RenderDryRun(MigrationPlan plan, bool showDiff)
    {
        Console.WriteLine();
        Console.WriteLine("=== WeApi Migration (Dry Run) ===");
        Console.WriteLine();
        Console.WriteLine($"  Project:             {plan.ProjectName}");
        Console.WriteLine($"  Project Type:        {plan.ProjectType}");
        Console.WriteLine($"  Current Framework:   {plan.CurrentFramework}");
        Console.WriteLine($"  Target Framework:    {plan.TargetFramework}");
        Console.WriteLine($"  Active SDK:          {plan.CurrentSdkVersion ?? "unknown"}");
        Console.WriteLine($"  Selected SDK:        {plan.SelectedSdkVersion ?? "compatible"}");

        Console.WriteLine();
        Console.WriteLine($"Files to modify ({plan.FileChanges.Count}):");
        foreach (var change in plan.FileChanges)
        {
            Console.WriteLine($"  • {Path.GetFileName(change.FilePath)} - {change.Description}");
        }

        if (plan.PackageChanges.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"Package version shifts ({plan.PackageChanges.Count}):");
            foreach (var pkg in plan.PackageChanges)
            {
                Console.WriteLine($"  • {pkg.PackageId}: {pkg.CurrentVersion} → {pkg.TargetVersion}");
            }
        }

        if (plan.Warnings.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Warnings:");
            foreach (var w in plan.Warnings)
            {
                Console.WriteLine($"  ⚠️  {w}");
            }
        }

        if (showDiff)
        {
            Console.WriteLine();
            Console.WriteLine("=== File Diffs ===");
            foreach (var change in plan.FileChanges)
            {
                Console.WriteLine();
                Console.WriteLine($"--- a/{Path.GetFileName(change.FilePath)}");
                Console.WriteLine($"+++ b/{Path.GetFileName(change.FilePath)}");

                var origLines = change.OriginalContent?.Split('\n') ?? Array.Empty<string>();
                var newLines = change.UpdatedContent.Split('\n');

                // Simple unified diff display for modified lines
                var minLen = Math.Min(origLines.Length, newLines.Length);
                for (int i = 0; i < minLen; i++)
                {
                    if (origLines[i].TrimEnd() != newLines[i].TrimEnd())
                    {
                        Console.WriteLine($"- {origLines[i].TrimEnd()}");
                        Console.WriteLine($"+ {newLines[i].TrimEnd()}");
                    }
                }
                for (int i = minLen; i < origLines.Length; i++)
                {
                    Console.WriteLine($"- {origLines[i].TrimEnd()}");
                }
                for (int i = minLen; i < newLines.Length; i++)
                {
                    Console.WriteLine($"+ {newLines[i].TrimEnd()}");
                }
            }
        }

        Console.WriteLine();
        ConsoleUi.WriteSuccess("[Dry Run] Analysis complete. No files were modified.");
    }

    private static void RenderSuccessSummary(MigrationPlan plan)
    {
        Console.WriteLine();
        Console.WriteLine("==========================================================");
        Console.WriteLine("  WeApi Migration Completed Successfully");
        Console.WriteLine("==========================================================");
        Console.WriteLine();
        Console.WriteLine($"  Project:          {plan.ProjectName}");
        Console.WriteLine($"  Target Framework: {plan.CurrentFramework} → {plan.TargetFramework}");
        Console.WriteLine($"  SDK:              {plan.SelectedSdkVersion ?? "compatible"} ✓");
        Console.WriteLine();
        Console.WriteLine($"  Files modified:   {plan.FileChanges.Count}");
        foreach (var change in plan.FileChanges)
        {
            Console.WriteLine($"    ✓ {Path.GetFileName(change.FilePath)}");
        }
        Console.WriteLine();
        Console.WriteLine($"  Verification:");
        Console.WriteLine($"    ✓ dotnet restore succeeded");
        Console.WriteLine($"    ✓ dotnet build succeeded");
        Console.WriteLine();
        ConsoleUi.WriteSuccess("Migration completed successfully.");
    }
}
