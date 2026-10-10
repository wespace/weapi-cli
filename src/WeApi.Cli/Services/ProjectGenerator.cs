using System.Collections.Frozen;
using System.Text.RegularExpressions;
using WeApi.Cli.Common;
using static WeApi.Cli.Common.FrameworkNormalizer;

namespace WeApi.Cli.Services;

public record ProjectGenerationOptions(
    string ProjectName,
    string? OutputDirectory,
    string Database = "sqlserver",
    string IdType = "guid",
    string Framework = "net10.0",
    bool DryRun = false);

public static class ProjectGenerator
{
    private static readonly Regex ValidNameRegex = new(
        @"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*$",
        RegexOptions.Compiled);

    private static readonly FrozenSet<string> SupportedDatabases = new[]
    {
        "sqlserver",
        "postgresql"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenSet<string> SupportedIdTypes = new[]
    {
        "guid",
        "int",
        "long"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static async Task<int> GenerateAsync(
        ProjectGenerationOptions options,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate project name
        if (string.IsNullOrWhiteSpace(options.ProjectName))
        {
            ConsoleUi.WriteError("Project name is required. Example: weapi new WeSpace.Api");
            return 1;
        }

        if (!ValidNameRegex.IsMatch(options.ProjectName))
        {
            ConsoleUi.WriteError(
                $"Project name '{options.ProjectName}' is invalid. It must be a valid C# namespace (e.g. 'WeSpace.Api' or 'InvoiceService').");
            return 1;
        }

        // 2. Validate database option
        var database = options.Database.ToLowerInvariant();
        if (!SupportedDatabases.Contains(database))
        {
            ConsoleUi.WriteError(
                $"Unsupported database provider '{options.Database}'. Supported options are: 'sqlserver', 'postgresql'.");
            return 1;
        }

        // 3. Validate entity ID type option
        var idType = options.IdType.ToLowerInvariant();
        if (!SupportedIdTypes.Contains(idType))
        {
            ConsoleUi.WriteError(
                $"Unsupported entity ID type '{options.IdType}'. Supported options are: 'guid', 'int', 'long'.");
            return 1;
        }

        // 4. Validate framework option
        if (!FrameworkNormalizer.TryNormalize(options.Framework, out var framework))
        {
            ConsoleUi.WriteError(
                $"Unsupported target framework '{options.Framework}'. Supported options are: {string.Join(", ", FrameworkNormalizer.GetSupportedFrameworks())}.");
            return 1;
        }

        // 5. Resolve target directory
        var targetDirectory = string.IsNullOrWhiteSpace(options.OutputDirectory)
            ? Path.Combine(Directory.GetCurrentDirectory(), options.ProjectName)
            : Path.GetFullPath(options.OutputDirectory);

        // 6. Validate target directory is empty if exists
        if (Directory.Exists(targetDirectory) && Directory.EnumerateFileSystemEntries(targetDirectory).Any())
        {
            ConsoleUi.WriteError(
                $"Project directory '{targetDirectory}' already exists and contains files.\nUse a different project name or specify another output directory with --output.");
            return 1;
        }

        if (options.DryRun)
        {
            ConsoleUi.WriteInfo($"[Dry Run] Would generate '{options.ProjectName}' in '{targetDirectory}' with '{database}' database, '{idType}' primary key ID type, and '{framework}' framework.");
            return 0;
        }

        ConsoleUi.WriteBanner();
        ConsoleUi.WriteInfo($"Creating {options.ProjectName} ({framework})...");
        Console.WriteLine();

        // 7. Ensure template is installed
        var templateInstalled = await TemplateManager.EnsureTemplateInstalledAsync(force: false, cancellationToken);
        if (!templateInstalled)
        {
            return 1;
        }

        // 8. Invoke dotnet new
        var arguments = $"new we-api -n \"{options.ProjectName}\" -o \"{targetDirectory}\" --database {database} --idType {idType} --framework {framework}";
        var result = await ProcessRunner.RunAsync("dotnet", arguments, null, cancellationToken);

        if (!result.Success)
        {
            ConsoleUi.WriteError($"Project generation failed:\n{result.StandardError}");
            return 1;
        }

        // 9. Report success with clear visual steps
        ConsoleUi.WriteStep("Solution created");
        ConsoleUi.WriteStep("API project created");
        ConsoleUi.WriteStep("Application project created");
        ConsoleUi.WriteStep("Domain project created");
        ConsoleUi.WriteStep("Infrastructure project created");
        ConsoleUi.WriteStep("Unit tests created");
        ConsoleUi.WriteStep("Integration tests created");
        ConsoleUi.WriteStep("JWT authentication configured");
        ConsoleUi.WriteStep($"Database configured ({database.ToUpperInvariant()})");
        ConsoleUi.WriteStep($"Entity ID type configured ({idType.ToUpperInvariant()})");
        ConsoleUi.WriteStep($"Target framework configured ({framework})");
        ConsoleUi.WriteStep("Swagger OpenAPI configured");

        ConsoleUi.WriteSuccess("Project created successfully.");
        ConsoleUi.WriteNextSteps(options.ProjectName, targetDirectory);

        return 0;
    }
}
