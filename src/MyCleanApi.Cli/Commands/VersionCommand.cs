using System.Reflection;
using System.Runtime.InteropServices;

namespace MyCleanApi.Cli.Commands;

public static class VersionCommand
{
    public static void Execute()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString(3) ?? "1.0.0";
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? version;

        Console.WriteLine($"MyCleanApi CLI v{informationalVersion}");
        Console.WriteLine($".NET Runtime:  {Environment.Version}");
        Console.WriteLine($"Platform:      {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})");
    }
}
