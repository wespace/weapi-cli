using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MyCompany.MyApi.Application.Interfaces;
using MyCompany.MyApi.Application.Services;

namespace MyCompany.MyApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<IAuthenticationService, AuthenticationService>();

        return services;
    }
}
