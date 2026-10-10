using System.Text.Json;
using System.Text.RegularExpressions;
using WeApi.Cli.Common;

namespace WeApi.Cli.Services;

public record SdkResolutionResult(
    string? ActiveSdkVersion,
    IReadOnlyList<string> InstalledSdks,
    bool GlobalJsonExists,
    string? GlobalJsonSdkVersion,
    string? GlobalJsonRollForward,
    bool GlobalJsonBlocksTarget,
    bool CanBuildTarget,
    string? BestCompatibleInstalledSdk);

public static class SdkResolver
{
    private static readonly Regex MajorVersionRegex = new(@"^(\d+)(?:\.|$)", RegexOptions.Compiled);

    private static readonly Regex TargetFrameworkMajorRegex = new(@"^(?:net)?(\d+)(?:\.|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static async Task<SdkResolutionResult> ResolveAsync(
        string solutionDirectory,
        string targetFramework,
        CancellationToken cancellationToken = default)
    {
        // 1. Get active SDK in the solution directory context
        var activeVersionResult = await ProcessRunner.RunAsync(
            "dotnet",
            "--version",
            solutionDirectory,
            cancellationToken);

        var activeSdkVersion = activeVersionResult.Success
            ? activeVersionResult.StandardOutput.Trim()
            : null;

        // 2. Query all installed SDKs on the machine
        var listSdksResult = await ProcessRunner.RunAsync(
            "dotnet",
            "--list-sdks",
            solutionDirectory,
            cancellationToken);

        var installedSdks = new List<string>();
        if (listSdksResult.Success)
        {
            var lines = listSdksResult.StandardOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split(' ', 2, StringSplitOptions.TrimEntries);
                if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
                {
                    installedSdks.Add(parts[0]);
                }
            }
        }

        // 3. Inspect global.json if present
        var globalJsonPath = Path.Combine(solutionDirectory, "global.json");
        var globalJsonExists = File.Exists(globalJsonPath);
        string? globalJsonVersion = null;
        string? globalJsonRollForward = null;

        if (globalJsonExists)
        {
            try
            {
                var jsonText = await File.ReadAllTextAsync(globalJsonPath, cancellationToken);
                using var doc = JsonDocument.Parse(jsonText);
                if (doc.RootElement.TryGetProperty("sdk", out var sdkElement))
                {
                    if (sdkElement.TryGetProperty("version", out var versionElement))
                    {
                        globalJsonVersion = versionElement.GetString();
                    }
                    if (sdkElement.TryGetProperty("rollForward", out var rollForwardElement))
                    {
                        globalJsonRollForward = rollForwardElement.GetString();
                    }
                }
            }
            catch
            {
                // Unreadable or malformed global.json
            }
        }

        // 4. Target major version
        var targetMajor = GetTargetMajor(targetFramework);

        // Find best installed SDK capable of building the target (major >= targetMajor)
        var bestCompatibleSdk = installedSdks
            .Where(v => GetSdkMajor(v) >= targetMajor)
            .OrderByDescending(ParseVersionSafe)
            .FirstOrDefault();

        // Check if the contextual active SDK can build the target
        var activeMajor = activeSdkVersion is not null ? GetSdkMajor(activeSdkVersion) : 0;
        var activeCanBuild = activeMajor >= targetMajor;

        // Check if global.json is explicitly locking to an older incompatible SDK
        var globalJsonBlocksTarget = false;
        if (globalJsonExists && !string.IsNullOrWhiteSpace(globalJsonVersion))
        {
            var globalMajor = GetSdkMajor(globalJsonVersion);
            if (globalMajor < targetMajor && !activeCanBuild)
            {
                globalJsonBlocksTarget = true;
            }
        }

        return new SdkResolutionResult(
            ActiveSdkVersion: activeSdkVersion,
            InstalledSdks: installedSdks,
            GlobalJsonExists: globalJsonExists,
            GlobalJsonSdkVersion: globalJsonVersion,
            GlobalJsonRollForward: globalJsonRollForward,
            GlobalJsonBlocksTarget: globalJsonBlocksTarget,
            CanBuildTarget: activeCanBuild || bestCompatibleSdk is not null,
            BestCompatibleInstalledSdk: bestCompatibleSdk);
    }

    public static int GetTargetMajor(string framework)
    {
        var match = TargetFrameworkMajorRegex.Match(framework);
        return match.Success && int.TryParse(match.Groups[1].Value, out var major) ? major : 10;
    }

    public static int GetSdkMajor(string sdkVersion)
    {
        var match = MajorVersionRegex.Match(sdkVersion);
        return match.Success && int.TryParse(match.Groups[1].Value, out var major) ? major : 0;
    }

    public static Version ParseVersionSafe(string versionStr)
    {
        var hyphenIndex = versionStr.IndexOf('-');
        var clean = hyphenIndex >= 0 ? versionStr.AsSpan(0, hyphenIndex).Trim() : versionStr.AsSpan().Trim();
        return Version.TryParse(clean, out var parsed) ? parsed : new Version(0, 0);
    }
}
