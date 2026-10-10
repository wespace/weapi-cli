using WeApi.Cli.Common;
using WeApi.Cli.Services;

namespace WeApi.Cli.Commands;

public static class UpgradeCommand
{
    public static async Task<int> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
    {
        string? targetPath = null;
        string? targetFramework = null;
        bool dryRun = false;
        bool showDiff = false;
        bool autoConfirm = false;
        bool installSdk = false;
        bool updateGlobalJson = false;

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("-h", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("-?", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                HelpCommand.Execute();
                return 0;
            }
            else if (arg.Equals("--framework", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("-f", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("--dotnet-version", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length)
                {
                    targetFramework = args[++i];
                }
                else
                {
                    ConsoleUi.WriteError("Missing value for option --framework. Allowed values: net6.0, net7.0, net8.0, net9.0, net10.0 (or 6, 7, 8, 9, 10)");
                    return 1;
                }
            }
            else if (arg.Equals("--path", StringComparison.OrdinalIgnoreCase) ||
                     arg.Equals("-p", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length)
                {
                    targetPath = args[++i];
                }
                else
                {
                    ConsoleUi.WriteError("Missing value for option --path.");
                    return 1;
                }
            }
            else if (arg.Equals("--dry-run", StringComparison.OrdinalIgnoreCase))
            {
                dryRun = true;
            }
            else if (arg.Equals("--diff", StringComparison.OrdinalIgnoreCase))
            {
                showDiff = true;
            }
            else if (arg.Equals("--yes", StringComparison.OrdinalIgnoreCase) ||
                     arg.Equals("-y", StringComparison.OrdinalIgnoreCase) ||
                     arg.Equals("--force", StringComparison.OrdinalIgnoreCase))
            {
                autoConfirm = true;
            }
            else if (arg.Equals("--install-sdk", StringComparison.OrdinalIgnoreCase))
            {
                installSdk = true;
            }
            else if (arg.Equals("--update-global-json", StringComparison.OrdinalIgnoreCase))
            {
                updateGlobalJson = true;
            }
            else if (arg.StartsWith("-"))
            {
                ConsoleUi.WriteError($"Unknown option '{arg}'. Run 'weapi --help' for usage.");
                return 1;
            }
            else if (targetPath is null)
            {
                targetPath = arg;
            }
            else
            {
                ConsoleUi.WriteError($"Unexpected argument '{arg}'. Run 'weapi --help' for usage.");
                return 1;
            }
        }

        if (string.IsNullOrWhiteSpace(targetFramework))
        {
            ConsoleUi.WriteError("Target framework is required.\nUsage: weapi upgrade [path] -f <net6.0|net7.0|net8.0|net9.0|net10.0> [--dry-run] [--diff] [--yes]");
            return 1;
        }

        var options = new MigrationOptions(
            TargetPath: targetPath,
            TargetFramework: targetFramework,
            DryRun: dryRun,
            ShowDiff: showDiff,
            AutoConfirm: autoConfirm,
            InstallSdk: installSdk,
            UpdateGlobalJson: updateGlobalJson);

        return await ProjectMigrator.MigrateAsync(options, cancellationToken);
    }
}
