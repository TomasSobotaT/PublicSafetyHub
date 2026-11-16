using System.Security.Cryptography.X509Certificates;
using PublicSafetyHub.Base.Extensions;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using OpenIddict.Abstractions;
using PublicSafetyHub.Base.Auth;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Data.Context;

namespace PublicSafetyHub.Host.BuilderExtensions;

public static class AuthServiceCollectionExtension
{
    public static IServiceCollection AddAuthRegistration(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddConfiguration<IAuthOptions, AuthOptions>(configuration, options =>
        {
            if (!Uri.TryCreate(options.Issuer, UriKind.Absolute, out var issuer) || issuer.Scheme != Uri.UriSchemeHttps)
            {
                throw new InvalidOperationException("AuthOptions:Issuer must be an absolute HTTPS URL.");
            }

            if (options.ClientIds is null || options.ClientIds.Length == 0 || options.ClientIds.Any(string.IsNullOrWhiteSpace))
            {
                throw new InvalidOperationException("AuthOptions:ClientIds must contain nonempty client identifiers.");
            }

            if (options.AccessTokenMinutes is <= 0 or > 60 || options.RefreshTokenDays is <= 0 or > 90)
            {
                throw new InvalidOperationException("Invalid token lifetime.");
            }

            if (!environment.IsDevelopment() && (string.IsNullOrWhiteSpace(options.SigningCertificatePath) || string.IsNullOrWhiteSpace(options.EncryptionCertificatePath)))
            {
                throw new InvalidOperationException("Configure AuthOptions:SigningCertificatePath and AuthOptions:EncryptionCertificatePath with their passwords using secrets for production.");
            }

            services.AddOpenIddict().AddServer(server =>
            {
                if (environment.IsDevelopment())
                {
                    server.AddDevelopmentSigningCertificate();
                    server.AddDevelopmentEncryptionCertificate();
                }
                else
                {
                    server.AddSigningCertificate(X509CertificateLoader.LoadPkcs12FromFile(options.SigningCertificatePath, options.SigningCertificatePassword));
                    server.AddEncryptionCertificate(X509CertificateLoader.LoadPkcs12FromFile(options.EncryptionCertificatePath, options.EncryptionCertificatePassword));
                }
            });
        });

        services.AddIdentityCore<UserEntity>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 12;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddRoles<IdentityRole<int>>()
        .AddSignInManager()
        .AddEntityFrameworkStores<PublicSafetyHubDbContext>();

        services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        services.AddAuthorization(options => options.AddPolicy("ApiAccess", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context => context.User.HasScope(AuthConstants.ApiScope));
        }));

        services.AddOpenIddict()
            .AddServer(options =>
            {
                options.SetTokenEndpointUris("/api/v1/auth/token", "/api/v1.0/auth/token");
                options.AllowPasswordFlow().AllowRefreshTokenFlow();
                options.SetRefreshTokenReuseLeeway(TimeSpan.Zero);
                options.RegisterScopes(AuthConstants.ApiScope);
                options.DisableAccessTokenEncryption();
                options.UseAspNetCore().EnableTokenEndpointPassthrough();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
                options.AddAudiences(AuthConstants.ApiResource);
                options.EnableTokenEntryValidation();
                options.EnableAuthorizationEntryValidation();
            });

        services.AddOptions<OpenIddict.Server.OpenIddictServerOptions>()
            .Configure<IAuthOptions>((serverOptions, authOptions) =>
            {
                serverOptions.Issuer = new Uri(authOptions.Issuer);
                serverOptions.AccessTokenLifetime = TimeSpan.FromMinutes(authOptions.AccessTokenMinutes);
                serverOptions.RefreshTokenLifetime = TimeSpan.FromDays(authOptions.RefreshTokenDays);
            });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("Auth", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 30,
                    Window = TimeSpan.FromMinutes(1),
                    AutoReplenishment = true,
                    QueueLimit = 0
                }));
            options.OnRejected = async (context, _) =>
            {
                await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too many requests. Please try again later."
                }, options: null, contentType: "application/problem+json");
            };
        });

        return services;
    }
}
