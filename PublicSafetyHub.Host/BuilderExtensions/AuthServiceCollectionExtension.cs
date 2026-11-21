using System.Security.Cryptography;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Validation.AspNetCore;
using OpenIddict.Abstractions;
using PublicSafetyHub.Base.OpenIddictAuth;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Core.Helpers;
using PublicSafetyHub.Data.Context;
using PublicSafetyHub.Host.Middlewares;
using PublicSafetyHub.Application.Services;
using PublicSafetyHub.Application.Services.Interfaces;

namespace PublicSafetyHub.Host.BuilderExtensions;

public static class AuthServiceCollectionExtension
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddAuthRegistration()
        {
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
                policy.AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context => context.User.HasScope(AuthConstants.ApiScope));
            }));

            services.AddOpenIddict()
                .AddServer(options =>
                {
                    options.SetTokenEndpointUris("/api/v1/auth/token", "/api/v1.0/auth/token");
                    options.SetAuthorizationEndpointUris("/connect/authorize");
                    options.AllowAuthorizationCodeFlow().AllowRefreshTokenFlow();
                    options.RequireProofKeyForCodeExchange();
                    options.Configure(serverOptions =>
                    {
                        serverOptions.CodeChallengeMethods.Clear();
                        serverOptions.CodeChallengeMethods.Add(OpenIddictConstants.CodeChallengeMethods.Sha256);
                    });
                    options.SetRefreshTokenReuseLeeway(TimeSpan.Zero);
                    options.RegisterScopes(AuthConstants.ApiScope, OpenIddictConstants.Scopes.OpenId,
                        OpenIddictConstants.Scopes.Roles, OpenIddictConstants.Scopes.OfflineAccess);
                    options.DisableAccessTokenEncryption();
                    options.UseAspNetCore().EnableAuthorizationEndpointPassthrough().EnableTokenEndpointPassthrough();
                })
                .AddValidation(options =>
                {
                    options.UseLocalServer();
                    options.UseAspNetCore()
                        .DisableAccessTokenExtractionFromBodyForm()
                        .DisableAccessTokenExtractionFromQueryString();
                    options.AddAudiences(AuthConstants.ApiResource);
                    options.EnableTokenEntryValidation();
                    options.EnableAuthorizationEntryValidation();
                });

            services.AddOptions<OpenIddict.Server.OpenIddictServerOptions>()
                .Configure<IAuthOptions>((serverOptions, authOptions) =>
                {
                    AuthConfigurationHelper.Validate(authOptions);

                    if (authOptions.AccessTokenMinutes is <= 0 or > 60 || authOptions.RefreshTokenDays is <= 0 or > 90)
                    {
                        throw new InvalidOperationException("Invalid token lifetime.");
                    }

                    if (string.IsNullOrWhiteSpace(authOptions.SigningPrivateKey) || string.IsNullOrWhiteSpace(authOptions.EncryptionKey))
                    {
                        throw new InvalidOperationException("Configure AuthOptions:SigningPrivateKey and AuthOptions:EncryptionKey.");
                    }

                    RsaSecurityKey signingKey;
                    byte[] encryptionKeyBytes;

                    try
                    {
                        using var rsa = RSA.Create();
                        rsa.ImportFromPem(authOptions.SigningPrivateKey);
                        signingKey = new RsaSecurityKey(rsa.ExportParameters(includePrivateParameters: true));
                        encryptionKeyBytes = Convert.FromBase64String(authOptions.EncryptionKey);
                    }
                    catch (Exception exception) when (exception is ArgumentException or CryptographicException or FormatException)
                    {
                        throw new InvalidOperationException("AuthOptions:SigningPrivateKey must be a PEM RSA private key and AuthOptions:EncryptionKey must be Base64 encoded.", exception);
                    }

                    if (signingKey.KeySize < 2048 || encryptionKeyBytes.Length != 32)
                    {
                        throw new InvalidOperationException("AuthOptions:SigningPrivateKey must contain at least 2048 bits and AuthOptions:EncryptionKey must contain exactly 32 bytes.");
                    }

                    serverOptions.SigningCredentials.Add(new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256));
                    serverOptions.EncryptionCredentials.Add(new EncryptingCredentials(new SymmetricSecurityKey(encryptionKeyBytes),
                        SecurityAlgorithms.Aes256KW, SecurityAlgorithms.Aes256CbcHmacSha512));
                    serverOptions.Issuer = new Uri(authOptions.Issuer);
                    serverOptions.AccessTokenLifetime = TimeSpan.FromMinutes(authOptions.AccessTokenMinutes);
                    serverOptions.RefreshTokenLifetime = TimeSpan.FromDays(authOptions.RefreshTokenDays);
                });

            services.AddSingleton<IAuthTransactionService, AuthTransactionService>();
            services.AddTransient<AuthorizationLoginMiddleware>();

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
                    context.HttpContext.Response.Headers.CacheControl = "no-store";
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds)
                            .ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }
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
}
