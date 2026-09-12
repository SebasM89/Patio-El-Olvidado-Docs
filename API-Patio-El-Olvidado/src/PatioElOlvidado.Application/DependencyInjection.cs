using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Application.Services;

namespace PatioElOlvidado.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProductoService, ProductoService>();
        services.AddValidatorsFromAssemblyContaining<AuthService>();
        return services;
    }
}
