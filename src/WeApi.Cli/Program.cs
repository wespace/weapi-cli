using WeApi.Cli.Commands;
using WeApi.Cli.Common;

namespace WeApi.Cli;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                HelpCommand.Execute();
                return 0;
            }

            var primaryCommand = args[0].ToLowerInvariant();

            return primaryCommand switch
            {
                "new" => await NewCommand.ExecuteAsync(args[1..]),
                "--version" or "-v" or "version" => ShowVersion(),
                "--help" or "-h" or "help" or "-?" => ShowHelp(),
                _ => HandleUnknownCommand(primaryCommand)
            };
        }
        catch (Exception ex)
        {
            ConsoleUi.WriteError($"An unexpected error occurred: {ex.Message}");
            return 1;
        }
    }

    private static int ShowVersion()
    {
        VersionCommand.Execute();
        return 0;
    }

    private static int ShowHelp()
    {
        HelpCommand.Execute();
        return 0;
    }

    private static int HandleUnknownCommand(string command)
    {
        ConsoleUi.WriteError($"Unknown command '{command}'.");
        HelpCommand.Execute();
        return 1;
    }
}
