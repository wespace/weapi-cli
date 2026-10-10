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
        string idType = "guid";
        string framework = "net10.0";
        bool dryRun = false;

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
            else if (arg.Equals("--database", StringComparison.OrdinalIgnoreCase) || arg.Equals("-d", StringComparison.OrdinalIgnoreCase))
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
            else if (arg.Equals("--id-type", StringComparison.OrdinalIgnoreCase) || arg.Equals("--id", StringComparison.OrdinalIgnoreCase) || arg.Equals("-i", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length)
                {
                    idType = args[++i];
                }
                else
                {
                    ConsoleUi.WriteError("Missing value for option --id-type. Allowed values: guid, int, long");
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
            else if (arg.Equals("--framework", StringComparison.OrdinalIgnoreCase) || arg.Equals("-f", StringComparison.OrdinalIgnoreCase) || arg.Equals("--dotnet-version", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length)
                {
                    framework = args[++i];
                }
                else
                {
                    ConsoleUi.WriteError("Missing value for option --framework. Allowed values: net6.0, net7.0, net8.0, net9.0, net10.0 (or 6, 7, 8, 9, 10)");
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
            ConsoleUi.WriteError("Project name is required.\nUsage: weapi new <ProjectName> [--database sqlserver|postgresql] [--id-type guid|int|long]");
            return 1;
        }

        var options = new ProjectGenerationOptions(
            ProjectName: projectName,
            OutputDirectory: outputDirectory,
            Database: database,
            IdType: idType,
            Framework: framework,
            DryRun: dryRun);

        return await ProjectGenerator.GenerateAsync(options, cancellationToken);
    }
}
