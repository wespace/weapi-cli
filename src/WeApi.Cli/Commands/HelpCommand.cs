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
  version, --version     Show version and runtime environment.
  help, --help, -h       Show help and command documentation.

NEW COMMAND OPTIONS:
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

EXAMPLES:
  # Generate project with SQL Server and GUID IDs (default)
  weapi new WeSpace.Api

  # Generate project with PostgreSQL and integer IDs
  weapi new OrderingService --database postgresql --id-type int

  # Generate project with long (bigint) IDs
  weapi new HighScaleService --id-type long

  # Generate into custom output directory
  weapi new InventoryApi -o ./services/inventory-api

For more documentation, visit:
https://github.com/weapi/dotnet-clean-api
");
    }
}
