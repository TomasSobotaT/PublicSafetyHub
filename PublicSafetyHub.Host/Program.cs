using Asp.Versioning.ApiExplorer;
using PublicSafetyHub.Host.BuilderExtensions;
using PublicSafetyHub.Data.Context;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddVersionedOpenApi();
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddDependencyInjectionRegistration(builder.Configuration);
builder.Services.AddHttpClientRegistration();
builder.Services.AddAuthRegistration();

var app = builder.Build();

if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("DevelopmentDatabase:Enabled"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<PublicSafetyHubDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().WithDocumentPerVersion();
    app.UseSwaggerUI(options =>
    {
        var versions = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        foreach (var version in versions.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint($"/openapi/{version.GroupName}.json", $"PublicSafetyHub API {version.GroupName}");
        }
    });
}

app.MapControllers();

app.Run();
