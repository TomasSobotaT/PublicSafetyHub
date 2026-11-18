using Microsoft.Extensions.Logging;
using Pgvector;
using PublicSafetyHub.AI.Services;
using PublicSafetyHub.AI.Services.Interfaces;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Base.UnitOfWork;
using PublicSafetyHub.Core.Configurations;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Core.Repositories;

namespace PublicSafetyHub.Application.Services;

public class ArticleEmbeddingService(
    IArticleRepository articleRepository,
    IEmbeddingService embeddingService,
    IUnitOfWork unitOfWork,
    IArticleEmbeddingOptions options,
    ILogger<ArticleEmbeddingService> logger)
    : IArticleEmbeddingService
{
    public async Task<int?> GenerateBatchAsync(int afterId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var articles = await articleRepository.GetArtíclesWithoutEmbeddingAsync(afterId, options.BatchSize);

        if (articles.Count == 0)
        {
            return null;
        }

        var validArticles = new List<ArticleEntity>();
        var texts = new List<string>();

        foreach (var article in articles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(article.Title) || string.IsNullOrWhiteSpace(article.Content))
            {
                logger.LogWarning("Skipping article {ArticleId} embedding: title or content is empty.", article.Id);
                continue;
            }

            var text = $"Title: {article.Title.Trim()}\n\n{article.Content.Trim()}";
            var tokenCount = embeddingService.CountTokens(text);

            if (tokenCount > EmbeddingService.MaxInputTokens)
            {
                logger.LogWarning("Skipping article {ArticleId} embedding: {TokenCount} tokens exceed {Limit}; the text was not truncated.",
                    article.Id, tokenCount, EmbeddingService.MaxInputTokens);
                continue;
            }

            validArticles.Add(article);
            texts.Add(text);
        }

        if (texts.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var vectors = await embeddingService.GenerateAsync(texts);
            cancellationToken.ThrowIfCancellationRequested();

            for (var index = 0; index < validArticles.Count; index++)
            {
                var article = validArticles[index];
                article.Embedding = new Vector(vectors[index].ToArray());
                articleRepository.Update(article);
            }

            cancellationToken.ThrowIfCancellationRequested();
            await unitOfWork.SaveChangesAsync();
        }

        logger.LogInformation("Article embedding batch finished: {Generated} generated, {Skipped} skipped, last ID {LastId}.",
            texts.Count, articles.Count - texts.Count, articles[^1].Id);

        // Advance even if the whole page is invalid, so later articles are still processed.
        return articles[^1].Id;
    }
}
