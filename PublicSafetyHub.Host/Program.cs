using Asp.Versioning.ApiExplorer;
using PublicSafetyHub.Host.BuilderExtensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});
builder.Services.AddVersionedOpenApi();
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddDependencyInjectionRegistration();
builder.Services.AddAuthRegistration(builder.Configuration, builder.Environment);

var app = builder.Build();

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
