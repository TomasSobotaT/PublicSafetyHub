using PublicSafetyHub.Host.BuilderExtensions;
using PublicSafetyHub.Host.Logging;
using PublicSafetyHub.Host.Middlewares;
using Serilog;
using Serilog.Debugging;

BootstrapLogging.Initialize();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddPostgreSqlLogging();
    builder.Services.AddGlobalExceptionHandling();
    builder.Services.AddControllers();
    builder.Services.AddOpenApiRegistration();
    builder.Services.AddDatabase(builder.Configuration);
    builder.Services.AddDependencyInjectionRegistration(builder.Configuration);
    builder.Services.AddHttpClientRegistration();
    builder.Services.AddAuthRegistration();
    builder.Services.AddAiRegistration();

    var app = builder.Build();

    app.UseExceptionHandler();
    app.UseRouting();
    app.UseRateLimiter();
    app.UseMiddleware<AuthorizationLoginMiddleware>();
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseOpenApi();
    app.MapControllers();

    await app.RunAsync();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Application terminated unexpectedly.");
    Environment.ExitCode = 1;
}
finally
{
    await Log.CloseAndFlushAsync();
    SelfLog.Disable();
}
