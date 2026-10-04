using WeSpace.Api.API.Extensions;
using WeSpace.Api.API.Middleware;
using WeSpace.Api.Application;
using WeSpace.Api.Infrastructure;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // 1. Logging
    builder.Services.AddSerilog((services, loggerConfiguration) =>
    {
        loggerConfiguration
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
    });

    // 2. Application & Infrastructure Layers
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // 3. API Services
    builder.Services.AddControllers();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();
    builder.Services.AddSwaggerDocumentation();
    builder.Services.AddCustomCors(builder.Configuration);

    var app = builder.Build();

    // 4. HTTP Request Pipeline
    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwaggerDocumentation();
    }
    else
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    app.UseCustomCors();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapCustomHealthChecks();

    Log.Information("Starting WeSpace.Api Web API host...");
    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application host terminated unexpectedly.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program
{
}
