namespace WeSpace.Api.API.Extensions;

public static class CorsExtensions
{
    public const string PolicyName = "AppCorsPolicy";

    public static IServiceCollection AddCustomCors(this IServiceCollection services, IConfiguration configuration)
    {
        var originsSection = configuration.GetSection("Cors:AllowedOrigins");
        var allowedOrigins = originsSection.Get<string[]>();

        // Support single string or comma-separated list from env vars (e.g. Cors__AllowedOrigins="*")
        if ((allowedOrigins is null || allowedOrigins.Length == 0) && !string.IsNullOrWhiteSpace(originsSection.Value))
        {
            allowedOrigins = originsSection.Value.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        allowedOrigins ??= ["http://localhost:3000", "http://localhost:5173"];

        var allowAnyOrigin = allowedOrigins.Length == 0
                             || allowedOrigins.Any(origin => origin.Trim() == "*");

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, builder =>
            {
                if (allowAnyOrigin)
                {
                    builder.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                }
                else
                {
                    builder.WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                }
            });
        });

        return services;
    }

    public static IApplicationBuilder UseCustomCors(this IApplicationBuilder app)
    {
        return app.UseCors(PolicyName);
    }
}
