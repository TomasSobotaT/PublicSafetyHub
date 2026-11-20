using PublicSafetyHub.Host.Logging;

namespace PublicSafetyHub.Host.BuilderExtensions;

public static class ExceptionHandlingExtension
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddGlobalExceptionHandling()
        {
            services.AddProblemDetails();
            services.AddExceptionHandler<GlobalExceptionHandler>();

            return services;
        }
    }
}
