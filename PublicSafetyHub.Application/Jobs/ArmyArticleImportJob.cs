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
        var settings = options;
        if (!settings.Enabled)
        {
            return;
        }

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nextRun = GetNextRunUtc(DateTime.UtcNow, settings, timeZone);
                logger.LogInformation("Next Army article import at {NextRun} ({TimeZone}).", TimeZoneInfo.ConvertTimeFromUtc(nextRun, timeZone), settings.TimeZoneId);
                // Recheck the clock while waiting, including after a suspended host resumes.
                while (DateTime.UtcNow < nextRun)
                {
                    var delay = nextRun - DateTime.UtcNow;
                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(delay < TimeSpan.FromMinutes(1) ? delay : TimeSpan.FromMinutes(1), stoppingToken);
                    }
                }

                await ImportAsync(stoppingToken);
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

    private async Task ImportAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Uri> urls;
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IArmyArticleImportService>();
            urls = await service.GetArticleUrlsAsync();
        }

        var imported = 0;
        var skipped = 0;
        var failed = 0;
        foreach (var url in urls)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<IArmyArticleImportService>();
                if (await service.ImportAsync(url))
                {
                    imported++;
                }
                else
                {
                    skipped++;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                failed++;
                logger.LogError(exception, "Army article import failed for {OriginUrl}.", url);
            }
        }

        logger.LogInformation("Army article import finished: {Imported} imported, {Skipped} existing, {Failed} failed.", imported, skipped, failed);
    }

    private static DateTime GetNextRunUtc(DateTime utcNow, IArmyArticleImportOptions settings, TimeZoneInfo timeZone)
    {
        var localDate = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone).Date;
        foreach (var date in new[] { localDate, localDate.AddDays(1) })
        {
            foreach (var time in new[] { settings.MorningTime, settings.EveningTime })
            {
                var localRun = DateTime.SpecifyKind(date.Add(time.ToTimeSpan()), DateTimeKind.Unspecified);
                // A configured time skipped by the spring clock change has no run that day.
                if (timeZone.IsInvalidTime(localRun))
                {
                    continue;
                }

                // During the autumn clock change, use the first occurrence exactly once.
                var utcRun = timeZone.IsAmbiguousTime(localRun)
                    ? new DateTimeOffset(localRun, timeZone.GetAmbiguousTimeOffsets(localRun).Max()).UtcDateTime
                    : TimeZoneInfo.ConvertTimeToUtc(localRun, timeZone);
                if (utcRun > utcNow)
                {
                    return utcRun;
                }
            }
        }

        throw new InvalidOperationException("No future Army import time was found.");
    }
}
