namespace WeApi.Cli.Services;

public enum ProjectType
{
    WeApiSolution,
    GenericSdkProject,
    Unsupported
}

public record FileMigrationChange(
    string FilePath,
    bool ExistedBefore,
    string? OriginalContent,
    string UpdatedContent,
    string Description);

public record PackageChange(
    string PackageId,
    string CurrentVersion,
    string TargetVersion);

public record MigrationPlan(
    string SolutionRoot,
    string? GitRoot,
    string ProjectName,
    ProjectType ProjectType,
    string CurrentFramework,
    string TargetFramework,
    string? CurrentSdkVersion,
    string? SelectedSdkVersion,
    bool RequiresSdkInstallation,
    bool UpdateGlobalJson,
    IReadOnlyList<string> ProjectFiles,
    IReadOnlyList<FileMigrationChange> FileChanges,
    IReadOnlyList<PackageChange> PackageChanges,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors,
    bool IsSupported,
    bool IsAlreadyTargeting);
