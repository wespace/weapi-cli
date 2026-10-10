namespace WeApi.Cli.Common;

public record GitStatusResult(
    string? GitRoot,
    bool IsGitRepository,
    bool HasUncommittedChanges,
    IReadOnlyList<string> ChangedFiles);

public static class GitHelper
{
    public static async Task<GitStatusResult> CheckStatusAsync(
        string workingDirectory,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Resolve GitRoot
            var rootResult = await ProcessRunner.RunAsync(
                "git",
                "rev-parse --show-toplevel",
                workingDirectory,
                cancellationToken);

            if (!rootResult.Success || string.IsNullOrWhiteSpace(rootResult.StandardOutput))
            {
                return new GitStatusResult(
                    GitRoot: null,
                    IsGitRepository: false,
                    HasUncommittedChanges: false,
                    ChangedFiles: Array.Empty<string>());
            }

            var gitRoot = Path.GetFullPath(rootResult.StandardOutput.Trim());

            // 2. Check porcelain status
            var statusResult = await ProcessRunner.RunAsync(
                "git",
                "status --porcelain",
                gitRoot,
                cancellationToken);

            if (!statusResult.Success)
            {
                return new GitStatusResult(
                    GitRoot: gitRoot,
                    IsGitRepository: true,
                    HasUncommittedChanges: false,
                    ChangedFiles: Array.Empty<string>());
            }

            var lines = statusResult.StandardOutput
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            return new GitStatusResult(
                GitRoot: gitRoot,
                IsGitRepository: true,
                HasUncommittedChanges: lines.Count > 0,
                ChangedFiles: lines);
        }
        catch
        {
            return new GitStatusResult(
                GitRoot: null,
                IsGitRepository: false,
                HasUncommittedChanges: false,
                ChangedFiles: Array.Empty<string>());
        }
    }
}
