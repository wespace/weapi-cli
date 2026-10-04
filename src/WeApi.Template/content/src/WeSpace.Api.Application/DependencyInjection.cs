using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using WeSpace.Api.Application.Interfaces;
using WeSpace.Api.Application.Services;

namespace WeSpace.Api.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<IAuthenticationService, AuthenticationService>();

        return services;
    }
}
