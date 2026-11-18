using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Core.Configurations;
using PublicSafetyHub.Core.Enums;

namespace PublicSafetyHub.Application.Jobs;

public class ArticleImportJob(
    IServiceScopeFactory scopeFactory,
    IArticleImportOptions options,
    IArmyArticleImportOptions armyOptions,
    IPoliceArticleImportOptions policeOptions,
    ILogger<ArticleImportJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!armyOptions.Enabled && !policeOptions.Enabled)
        {
            return;
        }

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.TimeZoneId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nextRun = GetNextRunUtc(DateTime.UtcNow, options, timeZone);
                logger.LogInformation("Next article import at {NextRun} ({TimeZone}).", TimeZoneInfo.ConvertTimeFromUtc(nextRun, timeZone), options.TimeZoneId);

                while (DateTime.UtcNow < nextRun)
                {
                    var delay = nextRun - DateTime.UtcNow;

                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(delay < TimeSpan.FromMinutes(1) ? delay : TimeSpan.FromMinutes(1), stoppingToken);
                    }
                }

                if (armyOptions.Enabled)
                {
                    await ImportAsync<IArmyArticleImportService>(ArticleType.Army, stoppingToken);
                }

                if (policeOptions.Enabled)
                {
                    await ImportAsync<IPoliceArticleImportService>(ArticleType.Police, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Article import scheduling failed. It will be retried in one minute.");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private async Task ImportAsync<TService>(ArticleType source, CancellationToken stoppingToken) where TService : class, IArticleImportService
    {
        try
        {
            stoppingToken.ThrowIfCancellationRequested();
            await using var scope = scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<TService>();
            await service.ImportAsync(stoppingToken);
        }
        catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
        {
            logger.LogError(exception, "{Source} article import failed. It will be retried at the next scheduled run.", source);
        }
    }

    private static DateTime GetNextRunUtc(DateTime dateTimeUtcNow, IArticleImportOptions settings, TimeZoneInfo timeZone)
    {
        var localDate = TimeZoneInfo.ConvertTimeFromUtc(dateTimeUtcNow, timeZone).Date;

        foreach (var date in new[] { localDate, localDate.AddDays(1) })
        {
            foreach (var time in new[] { settings.MorningTime, settings.EveningTime }.Distinct().Order())
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

        throw new InvalidOperationException("No future article import time was found.");
    }
}
