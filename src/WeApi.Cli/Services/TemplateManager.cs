using System.Reflection;
using WeApi.Cli.Common;

namespace WeApi.Cli.Services;

public static class TemplateManager
{
    private const string TemplateShortName = "we-api";
    private const string ResourceName = "WeApi.Template.nupkg";

    public static async Task<bool> IsTemplateInstalledAsync(CancellationToken cancellationToken = default)
    {
        var result = await ProcessRunner.RunAsync("dotnet", "new list we-api", null, cancellationToken);
        return result.Success && result.StandardOutput.Contains(TemplateShortName, StringComparison.OrdinalIgnoreCase);
    }

    public static async Task<bool> EnsureTemplateInstalledAsync(
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        if (!force && await IsTemplateInstalledAsync(cancellationToken))
        {
            return true;
        }

        // Extract embedded template package
        var tempDirectory = Path.Combine(Path.GetTempPath(), "weapi-template");
        Directory.CreateDirectory(tempDirectory);
        var tempPackagePath = Path.Combine(tempDirectory, "WeApi.Template.nupkg");

        var assembly = Assembly.GetExecutingAssembly();
        await using (var resourceStream = assembly.GetManifestResourceStream(ResourceName))
        {
            if (resourceStream is null)
            {
                // Fallback: search in adjacent directories if running from development build
                var resourcesDir = Path.Combine(AppContext.BaseDirectory, "Resources");
                var devPath = Directory.Exists(resourcesDir)
                    ? Directory.GetFiles(resourcesDir, "WeApi.Template*.nupkg").FirstOrDefault()
                    : null;

                if (devPath is not null && File.Exists(devPath))
                {
                    File.Copy(devPath, tempPackagePath, overwrite: true);
                }
                else
                {
                    ConsoleUi.WriteError($"Embedded template package '{ResourceName}' was not found in CLI assembly.");
                    return false;
                }
            }
            else
            {
                await using var fileStream = File.Create(tempPackagePath);
                await resourceStream.CopyToAsync(fileStream, cancellationToken);
            }
        }

        // Ensure no conflicting duplicate package paths exist before installing
        await ProcessRunner.RunAsync("dotnet", "new uninstall WeApi.Template", null, cancellationToken);

        // Install into dotnet new
        var installResult = await ProcessRunner.RunAsync(
            "dotnet",
            $"new install \"{tempPackagePath}\" --force",
            null,
            cancellationToken);

        if (!installResult.Success)
        {
            ConsoleUi.WriteError($"Failed to install template package: {installResult.StandardError}");
            return false;
        }

        return true;
    }
}
