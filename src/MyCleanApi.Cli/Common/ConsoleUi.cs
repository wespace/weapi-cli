namespace MyCleanApi.Cli.Common;

public static class ConsoleUi
{
    private static readonly bool SupportsAnsi = !Console.IsOutputRedirected;

    public static void WriteBanner()
    {
        WriteLine(ConsoleColor.Cyan, "==========================================================");
        WriteLine(ConsoleColor.Cyan, "  MyCleanApi CLI - Production-Ready .NET 10 Starter");
        WriteLine(ConsoleColor.Cyan, "==========================================================");
        Console.WriteLine();
    }

    public static void WriteStep(string step)
    {
        WriteLine(ConsoleColor.Green, $"  ✓ {step}");
    }

    public static void WriteInfo(string message)
    {
        WriteLine(ConsoleColor.Cyan, message);
    }

    public static void WriteSuccess(string message)
    {
        Console.WriteLine();
        WriteLine(ConsoleColor.Green, message);
    }

    public static void WriteWarning(string message)
    {
        WriteLine(ConsoleColor.Yellow, $"Warning: {message}");
    }

    public static void WriteError(string message)
    {
        Console.WriteLine();
        WriteLine(ConsoleColor.Red, $"Error: {message}");
    }

    public static void WriteNextSteps(string projectName, string projectDirectory)
    {
        Console.WriteLine();
        WriteLine(ConsoleColor.White, "Next steps:");
        Console.WriteLine();

        var relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), projectDirectory);
        if (relativePath != ".")
        {
            WriteLine(ConsoleColor.Yellow, $"  cd {relativePath}");
        }

        WriteLine(ConsoleColor.Yellow, "  dotnet restore");
        WriteLine(ConsoleColor.Yellow, "  dotnet build");
        WriteLine(ConsoleColor.Yellow, "  dotnet test");
        WriteLine(ConsoleColor.Yellow, $"  dotnet run --project src/{projectName}.API");
        Console.WriteLine();
        WriteLine(ConsoleColor.Gray, "Swagger UI will be available at: https://localhost:5001");
        WriteLine(ConsoleColor.Gray, "Health check endpoint:           https://localhost:5001/health");
        Console.WriteLine();
    }

    private static void WriteLine(ConsoleColor color, string message)
    {
        var originalColor = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
        }
        finally
        {
            Console.ForegroundColor = originalColor;
        }
    }
}
