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
    public async Task<int?> GenerateBatchAsync(int lastCheckedArticleId)
    {
        var articles = await articleRepository.GetArtíclesWithoutEmbeddingAsync(lastCheckedArticleId, options.BatchSize);

        if (articles.Count == 0)
        {
            return null;
        }

        var validArticles = new List<ArticleEntity>();
        var textToEmbeddingDictionary = new Dictionary<int, string>();

        foreach (var article in articles)
        {
            if (string.IsNullOrWhiteSpace(article.Title) || string.IsNullOrWhiteSpace(article.Content))
            {
                logger.LogWarning("Skipping article {ArticleId} embedding: title or content is empty.", article.Id);
                continue;
            }

            var textToEmbedding = $"Title: {article.Title.Trim()}\n\n{article.Content.Trim()}";
            var tokenCount = embeddingService.GetTokenCount(textToEmbedding);

            if (tokenCount > EmbeddingService.MaxInputTokens)
            {
                logger.LogWarning("Skipping article {ArticleId} embedding: {TokenCount} tokens exceed {Limit}; the text was not truncated.", article.Id, tokenCount, EmbeddingService.MaxInputTokens);
                continue;
            }

            validArticles.Add(article);
            textToEmbeddingDictionary.Add(article.Id, textToEmbedding);
        }

        if (textToEmbeddingDictionary.Count > 0)
        {
            var vectorDictionary = await embeddingService.GenerateAsync(textToEmbeddingDictionary);
            foreach (var article in validArticles)
            {
                article.Embedding = new Vector(vectorDictionary[article.Id]);
                articleRepository.Update(article);
            }

            await unitOfWork.SaveChangesAsync();
        }

        logger.LogInformation("Article embedding batch finished: {Generated} generated, {Skipped} skipped, last ID {LastId}.", textToEmbeddingDictionary.Count, articles.Count - textToEmbeddingDictionary.Count, articles.Last().Id);

        return articles.Last().Id;
    }
}
