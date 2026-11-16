using FluentValidation;
using PublicSafetyHub.Application.Services;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Application.Validators.Auth;
using PublicSafetyHub.Base.UserContext;
using PublicSafetyHub.Data.Repositories;
using PublicSafetyHub.Data.Repositories.Interfaces;

namespace PublicSafetyHub.Host.BuilderExtensions;

public static class DependencyInjectionExtension
{
    public static IServiceCollection AddDependencyInjectionRegistration(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddValidatorsFromAssemblyContaining<RegisterUserRequestValidator>();
        services.AddScoped<IHttpUserContext, HttpUserContext>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddHostedService<AuthClientInitializer>();

        return services;
    }
}
