using WeSpace.Api.API.Middleware;

namespace WeSpace.Api.API.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers presentation and API layer services including controllers, exception handling,
    /// problem details, Swagger documentation, and CORS.
    /// </summary>
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        services.AddSwaggerDocumentation();
        services.AddCustomCors(configuration);

        return services;
    }
}
