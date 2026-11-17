using FluentValidation;
using PublicSafetyHub.Application.Services;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Base.UserContext;
using PublicSafetyHub.Base.UnitOfWork;
using PublicSafetyHub.Data.Repositories;
using PublicSafetyHub.Data.Repositories.Interfaces;
using PublicSafetyHub.Data.UnitOfWork;
using PublicSafetyHub.Application.Validators;
using PublicSafetyHub.Application.Mappings;

namespace PublicSafetyHub.Host.BuilderExtensions;

public static class DependencyInjectionExtension
{
    public static IServiceCollection AddDependencyInjectionRegistration(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddValidatorsFromAssemblyContaining<RegisterUserRequestValidator>();
        services.AddAutoMapper(typeof(EntityMappingProfile));
        services.AddScoped<IHttpUserContext, HttpUserContext>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IArticleService, ArticleService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IArticleRepository, ArticleRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddHostedService<AuthClientInitializer>();

        return services;
    }
}
