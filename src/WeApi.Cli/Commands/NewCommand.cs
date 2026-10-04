using WeApi.Cli.Common;
using WeApi.Cli.Services;

namespace WeApi.Cli.Commands;

public static class NewCommand
{
    public static async Task<int> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
    {
        string? projectName = null;
        string? outputDirectory = null;
        string database = "sqlserver";
        bool dryRun = false;

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg.Equals("--database", StringComparison.OrdinalIgnoreCase) || arg.Equals("-d", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length)
                {
                    database = args[++i];
                }
                else
                {
                    ConsoleUi.WriteError("Missing value for option --database. Allowed values: sqlserver, postgresql");
                    return 1;
                }
            }
            else if (arg.Equals("--output", StringComparison.OrdinalIgnoreCase) || arg.Equals("-o", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length)
                {
                    outputDirectory = args[++i];
                }
                else
                {
                    ConsoleUi.WriteError("Missing value for option --output.");
                    return 1;
                }
            }
            else if (arg.Equals("--dry-run", StringComparison.OrdinalIgnoreCase))
            {
                dryRun = true;
            }
            else if (arg.StartsWith("-"))
            {
                ConsoleUi.WriteError($"Unknown option '{arg}'. Run 'weapi --help' for usage.");
                return 1;
            }
            else if (projectName is null)
            {
                projectName = arg;
            }
            else
            {
                ConsoleUi.WriteError($"Unexpected argument '{arg}'. Run 'weapi --help' for usage.");
                return 1;
            }
        }

        if (string.IsNullOrWhiteSpace(projectName))
        {
            ConsoleUi.WriteError("Project name is required.\nUsage: weapi new <ProjectName> [--database sqlserver|postgresql]");
            return 1;
        }

        var options = new ProjectGenerationOptions(
            ProjectName: projectName,
            OutputDirectory: outputDirectory,
            Database: database,
            DryRun: dryRun);

        return await ProjectGenerator.GenerateAsync(options, cancellationToken);
    }
}
