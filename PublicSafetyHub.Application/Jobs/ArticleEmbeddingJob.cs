using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PublicSafetyHub.Core.Constants;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Core.Configurations;

namespace PublicSafetyHub.Application.Jobs;

public class ArticleEmbeddingJob(
    IServiceScopeFactory scopeFactory,
    IArticleEmbeddingOptions articleEmbeddingOptions,
    IOpenAiOptions openAiOptions,
    ILogger<ArticleEmbeddingJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!articleEmbeddingOptions.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(openAiOptions.ApiKey))
        {
            logger.LogWarning("Article embeddings are enabled but OpenAiOptions:ApiKey is missing or provisional. Configure user secrets or environment variables and restart the application.");
            return;
        }

        if (HasInvalidConfiguration())
        {
            logger.LogError("Invalid article embedding settings: use an interval from 1 ms to 49 days, a batch size of 1 to {MaxBatchSize}, and a model name up to 100 characters.", EmbeddingConstants.MaxBatchSize);
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromHours(articleEmbeddingOptions.IntervalHours));

        try
        {
            do
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    return;
                }

                await GenerateMissingEmbeddingsAsync();
                logger.LogInformation("Article embedding check finished; configured interval is {IntervalHours} hours.", articleEmbeddingOptions.IntervalHours);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Article embedding job stopped because the application is shutting down.");
        }
    }

    private bool HasInvalidConfiguration()
    {
        return !double.IsFinite(articleEmbeddingOptions.IntervalHours)
            || articleEmbeddingOptions.IntervalHours < TimeSpan.FromMilliseconds(1).TotalHours
            || articleEmbeddingOptions.IntervalHours > 24 * 49
            || articleEmbeddingOptions.BatchSize < 1
            || articleEmbeddingOptions.BatchSize > EmbeddingConstants.MaxBatchSize
            || string.IsNullOrWhiteSpace(openAiOptions.EmbeddingModel)
            || openAiOptions.EmbeddingModel.Length > 100;
    }

    private async Task GenerateMissingEmbeddingsAsync()
    {
        using var logScope = logger.BeginScope(new Dictionary<string, object>
        {
            ["EmbeddingRunId"] = Guid.NewGuid().ToString("N")
        });

        try
        {
            var lastCheckedArticleId = 0;
            while (true)
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var articleEmbeddingService = scope.ServiceProvider.GetRequiredService<IArticleEmbeddingService>();
                var lastArticleIdInBatch = await articleEmbeddingService.GenerateBatchAsync(lastCheckedArticleId);

                if (!lastArticleIdInBatch.HasValue)
                {
                    return;
                }

                lastCheckedArticleId = lastArticleIdInBatch.Value;
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Article embedding run failed. Missing embeddings will be retried at the next scheduled check.");
        }
    }
}
