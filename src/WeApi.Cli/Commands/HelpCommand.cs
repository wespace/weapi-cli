namespace WeApi.Cli.Commands;

public static class HelpCommand
{
    public static void Execute()
    {
        Console.WriteLine(@"
WeApi CLI - Production-Ready Clean Architecture .NET 10 Starter

USAGE:
  weapi [command] [arguments] [options]

COMMANDS:
  new <ProjectName>      Generate a new Clean Architecture Web API project.
  upgrade [path]         Upgrade or downgrade target framework of an existing project.
  version, --version     Show version and runtime environment.
  help, --help, -h       Show help and command documentation.

NEW COMMAND OPTIONS:
  -f, --framework <tfm>  Target .NET framework:
                         'net10.0' (default), 'net9.0', 'net8.0', 'net7.0', 'net6.0' (or 10, 9, 8, 7, 6)
  -d, --database <db>    Relational database provider:
                         'sqlserver'  - Microsoft SQL Server (default)
                         'postgresql' - PostgreSQL via Npgsql
  -i, --id-type <type>   Primary key identifier type:
                         'guid' - GUID / UUID v7 (default, recommended)
                         'int'  - 32-bit integer identity
                         'long' - 64-bit bigint identity
  -o, --output <dir>     Output directory for the generated solution.
                         Default: ./<ProjectName>
  --dry-run              Display what would be created without writing files.

UPGRADE COMMAND OPTIONS:
  -f, --framework <tfm>  Target framework to migrate to (required):
                         'net6.0', 'net7.0', 'net8.0', 'net9.0', 'net10.0' (or 6, 7, 8, 9, 10)
  -p, --path <path>      Path to project or solution directory (optional if in current dir).
  --dry-run              Simulate migration analysis without modifying any files.
  --diff                 Show unified diff of file changes (works with --dry-run).
  -y, --yes, --force     Bypass interactive confirmation prompts.
  --install-sdk          Automatically download and install missing .NET SDK if needed.
  --update-global-json   Update global.json SDK version to match an installed compatible SDK.

EXAMPLES:
  # Generate project with .NET 10 (default)
  weapi new WeSpace.Api

  # Generate project targeting .NET 8 LTS with PostgreSQL
  weapi new OrderingService -f net8.0 --database postgresql --id-type int

  # Upgrade current solution to .NET 10
  weapi upgrade -f net10.0

  # Downgrade project in specific folder to .NET 8 (with dry-run preview)
  weapi upgrade ./services/inventory-api -f net8.0 --dry-run --diff

  # Upgrade with automated confirmation and SDK installation
  weapi upgrade -f net10.0 --yes --install-sdk

For more documentation, visit:
https://github.com/wespace/weapi-cli
");
    }
}
