using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Core.Configurations;

namespace PublicSafetyHub.Application.Jobs;

public class ArmyArticleImportJob(IServiceScopeFactory scopeFactory, IArmyArticleImportOptions options, ILogger<ArmyArticleImportJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enabled)
        {
            return;
        }

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.TimeZoneId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nextRun = GetNextRunUtc(DateTime.UtcNow, options, timeZone);
                logger.LogInformation("Next Army article import at {NextRun} ({TimeZone}).", TimeZoneInfo.ConvertTimeFromUtc(nextRun, timeZone), options.TimeZoneId);

                while (DateTime.UtcNow < nextRun)
                {
                    var delay = nextRun - DateTime.UtcNow;

                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(delay < TimeSpan.FromMinutes(1) ? delay : TimeSpan.FromMinutes(1), stoppingToken);
                    }
                }

                await using var scope = scopeFactory.CreateAsyncScope();
                var armyArticleImportService = scope.ServiceProvider.GetRequiredService<IArmyArticleImportService>();

                await armyArticleImportService.ImportFeedAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Army feed import failed. It will be retried at the next scheduled run.");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private static DateTime GetNextRunUtc(DateTime dateTimeUtcNow, IArmyArticleImportOptions settings, TimeZoneInfo timeZone)
    {
        var localDate = TimeZoneInfo.ConvertTimeFromUtc(dateTimeUtcNow, timeZone).Date;

        foreach (var date in new[] { localDate, localDate.AddDays(1) })
        {
            foreach (var time in new[] { settings.MorningTime, settings.EveningTime })
            {
                var localRun = DateTime.SpecifyKind(date.Add(time.ToTimeSpan()), DateTimeKind.Unspecified);

                if (timeZone.IsInvalidTime(localRun))
                {
                    continue;
                }

                var utcRun = timeZone.IsAmbiguousTime(localRun)
                    ? new DateTimeOffset(localRun, timeZone.GetAmbiguousTimeOffsets(localRun).Max()).UtcDateTime
                    : TimeZoneInfo.ConvertTimeToUtc(localRun, timeZone);

                if (utcRun > dateTimeUtcNow)
                {
                    return utcRun;
                }
            }
        }

        throw new InvalidOperationException("No future Army import time was found.");
    }
}
