using System.Runtime.InteropServices;
using WeApi.Cli.Common;

namespace WeApi.Cli.Services;

public static class SdkInstaller
{
    public static async Task<bool> InstallSdkAsync(
        string targetFramework,
        CancellationToken cancellationToken = default)
    {
        var targetMajor = SdkResolver.GetTargetMajor(targetFramework);
        var channel = $"{targetMajor}.0";

        ConsoleUi.WriteInfo($"Downloading and installing official .NET {channel} SDK from Microsoft...");

        ProcessResult result;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var command =
                $"& {{ [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; " +
                $"&([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing 'https://dot.net/v1/dotnet-install.ps1'))) " +
                $"-Channel {channel} -Quality ga }}";

            result = await ProcessRunner.RunAsync(
                "powershell",
                $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
                null,
                cancellationToken);
        }
        else
        {
            var command = $"curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel {channel} --quality ga";

            result = await ProcessRunner.RunAsync(
                "bash",
                $"-c \"{command}\"",
                null,
                cancellationToken);
        }

        if (!result.Success)
        {
            ConsoleUi.WriteError($"Failed to install .NET {channel} SDK: {result.StandardError}");
            return false;
        }

        // Post-install verification
        var verifyResult = await ProcessRunner.RunAsync("dotnet", "--list-sdks", null, cancellationToken);
        var installed = verifyResult.Success && verifyResult.StandardOutput.Contains($"{channel}.");

        if (installed)
        {
            ConsoleUi.WriteStep($".NET {channel} SDK installed and verified");
            return true;
        }

        ConsoleUi.WriteWarning(
            $"The installer completed, but the .NET {channel} SDK may require a terminal restart to appear on PATH.");
        return true;
    }
}
