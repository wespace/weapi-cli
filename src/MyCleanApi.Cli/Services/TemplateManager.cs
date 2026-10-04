using System.Reflection;
using MyCleanApi.Cli.Common;

namespace MyCleanApi.Cli.Services;

public static class TemplateManager
{
    private const string TemplateShortName = "my-clean-api";
    private const string ResourceName = "MyCleanApi.Template.nupkg";

    public static async Task<bool> IsTemplateInstalledAsync(CancellationToken cancellationToken = default)
    {
        var result = await ProcessRunner.RunAsync("dotnet", "new list my-clean-api", null, cancellationToken);
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
        var tempDirectory = Path.Combine(Path.GetTempPath(), "mycleanapi-template");
        Directory.CreateDirectory(tempDirectory);
        var tempPackagePath = Path.Combine(tempDirectory, "MyCleanApi.Template.1.0.0.nupkg");

        var assembly = Assembly.GetExecutingAssembly();
        await using (var resourceStream = assembly.GetManifestResourceStream(ResourceName))
        {
            if (resourceStream is null)
            {
                // Fallback: search in adjacent directories if running from development build
                var devPath = Path.Combine(AppContext.BaseDirectory, "Resources", "MyCleanApi.Template.1.0.0.nupkg");
                if (File.Exists(devPath))
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
