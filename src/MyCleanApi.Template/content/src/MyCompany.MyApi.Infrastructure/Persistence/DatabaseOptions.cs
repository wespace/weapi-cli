namespace MyCompany.MyApi.Infrastructure.Persistence;

public class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// Database provider: "SqlServer" or "PostgreSql"
    /// </summary>
    public string Provider { get; set; } = "SqlServer";

    /// <summary>
    /// Connection string name or direct connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
}
