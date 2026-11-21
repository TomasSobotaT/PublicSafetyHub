using FluentValidation;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PublicSafetyHub.Application.Jobs;
using PublicSafetyHub.Application.Services;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Application.Validators;
using PublicSafetyHub.Application.UserContext;
using PublicSafetyHub.Base.OpenIddictAuth;
using PublicSafetyHub.Base.Extensions;
using PublicSafetyHub.Core.Configurations;
using PublicSafetyHub.Base.UserContext;
using PublicSafetyHub.Base.UnitOfWork;
using PublicSafetyHub.Data.Repositories;
using PublicSafetyHub.Core.Repositories;
using PublicSafetyHub.Data.Context;
using PublicSafetyHub.Integration.Repositories.Interfaces;
using PublicSafetyHub.Integration.Repositories;

namespace PublicSafetyHub.Host.BuilderExtensions;

public static class DependencyInjectionExtension
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDependencyInjectionRegistration(IConfiguration configuration)
        {
            services.RegisterConfigurations(configuration);
            services.RegisterServices();
            services.RegisterRepositories();
            services.RegisterValidators();
            services.RegisterMappings();
            services.RegisterHostedServices();

            return services;
        }

        private void RegisterServices()
        {
            services.AddHttpContextAccessor();
            services.TryAddScoped<IUserContext, UserContext>();
            services.TryAddScoped<IHttpUserContext>(provider => provider.GetRequiredService<IUserContext>());
            services.TryAddScoped<IAuthService, AuthService>();
            services.TryAddScoped<IArticleService, ArticleService>();
            services.TryAddScoped<ILogService, LogService>();
            services.TryAddScoped<IUserService, UserService>();
            services.TryAddScoped<IArticleEmbeddingService, ArticleEmbeddingService>();
            services.TryAddScoped<IArmyArticleImportService, ArmyArticleImportService>();
            services.TryAddScoped<IPoliceArticleImportService, PoliceArticleImportService>();
            services.TryAddScoped<IFirefightersArticleImportService, FirefightersArticleImportService>();
            services.TryAddScoped<ITokenService, TokenService>();
        }

        private void RegisterRepositories()
        {
            services.TryAddScoped<IUserRepository, UserRepository>();
            services.TryAddScoped<IArticleRepository, ArticleRepository>();
            services.TryAddScoped<ILogRepository, LogRepository>();
            services.TryAddScoped<IArmyArticlesRepository, ArmyArticlesRepository>();
            services.TryAddScoped<IPoliceArticlesRepository, PoliceArticlesRepository>();
            services.TryAddScoped<IFirefightersArticlesRepository, FirefightersArticlesRepository>();
            services.TryAddScoped<IUnitOfWork>(provider => new UnitOfWork(provider.GetRequiredService<PublicSafetyHubDbContext>()));
        }

        private void RegisterValidators()
        {
            services.AddValidatorsFromAssemblyContaining<RegisterUserRequestValidator>();
        }

        private void RegisterMappings()
        {
            services.AddAutoMapper(_ => { }, typeof(AutomapperProfile));
        }

        private void RegisterHostedServices()
        {
            services.AddHostedService<AuthClientInitializer>();
            services.AddHostedService<ArticleImportJob>();
            services.AddHostedService<ArticleEmbeddingJob>();
        }

        private void RegisterConfigurations(IConfiguration configuration)
        {
            services.AddConfiguration<IArmyArticleImportOptions, ArmyArticleImportOptions>(configuration);
            services.AddConfiguration<IPoliceArticleImportOptions, PoliceArticleImportOptions>(configuration);
            services.AddConfiguration<IFirefightersArticleImportOptions, FirefightersArticleImportOptions>(configuration);
            services.AddConfiguration<IArticleImportOptions, ArticleImportOptions>(configuration);
            services.AddConfiguration<IOpenAiOptions, OpenAiOptions>(configuration);
            services.AddConfiguration<IArticleEmbeddingOptions, ArticleEmbeddingOptions>(configuration);
            services.AddConfiguration<IAuthOptions, AuthOptions>(configuration);
        }
    }
}
