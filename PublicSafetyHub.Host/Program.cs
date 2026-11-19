using PublicSafetyHub.Host.BuilderExtensions;
using PublicSafetyHub.Data.Context;
using PublicSafetyHub.Host.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApiRegistration();
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddDependencyInjectionRegistration(builder.Configuration);
builder.Services.AddHttpClientRegistration();
builder.Services.AddAuthRegistration();
builder.Services.AddAiRegistration();

var app = builder.Build();

if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("DevelopmentDatabase:Enabled"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<PublicSafetyHubDbContext>();
    await dbContext.Database.EnsureDeletedAsync();
    await dbContext.Database.EnsureCreatedAsync();
}

app.UseRouting();
app.UseRateLimiter();
app.UseMiddleware<AuthorizationLoginMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseOpenApi();
app.MapControllers();
app.Run();
