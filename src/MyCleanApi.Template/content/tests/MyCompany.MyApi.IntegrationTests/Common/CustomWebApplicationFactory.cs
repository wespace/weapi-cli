using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MyCompany.MyApi.Infrastructure.Persistence;

namespace MyCompany.MyApi.IntegrationTests.Common;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly string DbName = $"IntegrationTestDb_{Guid.NewGuid()}";

    static CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("Database__Provider", "InMemory");
        Environment.SetEnvironmentVariable("Database__ConnectionString", DbName);
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", DbName);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }

    public void EnsureDatabaseCreated()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();
    }
}
