namespace MyCleanApi.Cli.Commands;

public static class HelpCommand
{
    public static void Execute()
    {
        Console.WriteLine(@"
MyCleanApi CLI - Production-Ready Clean Architecture .NET 10 Starter

USAGE:
  mycleanapi [command] [arguments] [options]

COMMANDS:
  new <ProjectName>      Generate a new Clean Architecture Web API project.
  version, --version     Show version and runtime environment.
  help, --help, -h       Show help and command documentation.

NEW COMMAND OPTIONS:
  -d, --database <db>    Relational database provider:
                         'sqlserver'  - Microsoft SQL Server (default)
                         'postgresql' - PostgreSQL via Npgsql
  -o, --output <dir>     Output directory for the generated solution.
                         Default: ./<ProjectName>
  --dry-run              Display what would be created without writing files.

EXAMPLES:
  # Generate project with SQL Server
  mycleanapi new MyCompany.MyApi

  # Generate project with PostgreSQL
  mycleanapi new OrderingService --database postgresql

  # Generate into custom output directory
  mycleanapi new InventoryApi -o ./services/inventory-api

For more documentation, visit:
https://github.com/mycleanapi/dotnet-clean-api
");
    }
}
