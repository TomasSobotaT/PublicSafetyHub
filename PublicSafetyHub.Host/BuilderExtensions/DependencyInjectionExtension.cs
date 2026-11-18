using FluentValidation;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PublicSafetyHub.Application.Jobs;
using PublicSafetyHub.Application.Mappings;
using PublicSafetyHub.Application.Services;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Application.Validators;
using PublicSafetyHub.Base.Auth;
using PublicSafetyHub.Base.Extensions;
using PublicSafetyHub.Core.Configurations;
using PublicSafetyHub.Base.UserContext;
using PublicSafetyHub.Base.UnitOfWork;
using PublicSafetyHub.Data.Repositories;
using PublicSafetyHub.Core.Repositories;
using PublicSafetyHub.Data.UnitOfWork;
using PublicSafetyHub.Core.Repositories.Integration;
using PublicSafetyHub.Integration.Repositories;

namespace PublicSafetyHub.Host.BuilderExtensions;

public static class DependencyInjectionExtension
{
    public static IServiceCollection AddDependencyInjectionRegistration(this IServiceCollection services, IConfiguration configuration)
    {
        services.RegisterConfigurations(configuration);
        services.RegisterServices();
        services.RegisterRepositories();
        services.RegisterValidators();
        services.RegisterMappings();
        services.RegisterHostedServices();

        return services;
    }

    private static void RegisterServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.TryAddScoped<IHttpUserContext, HttpUserContext>();
        services.TryAddScoped<IAuthService, AuthService>();
        services.TryAddScoped<IArticleService, ArticleService>();
        services.TryAddScoped<IArmyArticleImportService, ArmyArticleImportService>();
        services.TryAddScoped<IPoliceArticleImportService, PoliceArticleImportService>();
        services.TryAddScoped<ITokenService, TokenService>();
    }

    private static void RegisterRepositories(this IServiceCollection services)
    {
        services.TryAddScoped<IUserRepository, UserRepository>();
        services.TryAddScoped<IArticleRepository, ArticleRepository>();
        services.TryAddScoped<IArmyArticlesRepository, ArmyArticlesRepository>();
        services.TryAddScoped<IPoliceArticlesRepository, PoliceArticlesRepository>();
        services.TryAddScoped<IUnitOfWork, UnitOfWork>();
    }

    private static void RegisterValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterUserRequestValidator>();
    }

    private static void RegisterMappings(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(AutomapperProfile));
    }

    private static void RegisterHostedServices(this IServiceCollection services)
    {
        services.AddHostedService<AuthClientInitializer>();
        services.AddHostedService<ArticleImportJob>();
    }

    private static void RegisterConfigurations(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddConfiguration<IArmyArticleImportOptions, ArmyArticleImportOptions>(configuration);
        services.AddConfiguration<IPoliceArticleImportOptions, PoliceArticleImportOptions>(configuration);
        services.AddConfiguration<IArticleImportOptions, ArticleImportOptions>(configuration);
        services.AddConfiguration<IAuthOptions, AuthOptions>(configuration);
    }
}
