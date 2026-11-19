using FluentValidation;
using Microsoft.Extensions.Logging;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Base.UnitOfWork;
using PublicSafetyHub.Core.Configurations;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Core.Repositories;
using PublicSafetyHub.Core.Repositories.Integration;
using PublicSafetyHub.Models.Articles;
using PublicSafetyHub.Models.Enums;

namespace PublicSafetyHub.Application.Services;

public class FirefightersArticleImportService(
    IFirefightersArticlesRepository firefightersArticlesRepository,
    IArticleRepository articleRepository,
    IUnitOfWork unitOfWork,
    IValidator<FirefightersArticle> validator,
    IFirefightersArticleImportOptions options,
    ILogger<FirefightersArticleImportService> logger) : IFirefightersArticleImportService
{
    public async Task ImportAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(options.Username) || string.IsNullOrWhiteSpace(options.Password))
        {
            logger.LogWarning("Firefighters article import skipped: IMAP credentials are missing.");
            return;
        }

        var links = await firefightersArticlesRepository.GetArticleLinksFromEmailNewslettersAsync(cancellationToken);
        var distinctLinks = links.Distinct(StringComparer.Ordinal).ToArray();
        var existingUrls = await articleRepository.GetExistingOriginUrlsAsync(distinctLinks, cancellationToken);

        var imported = 0;
        var existing = 0;
        var failed = 0;

        var knownUrls = existingUrls.ToHashSet(StringComparer.Ordinal);

        foreach (var originUrl in distinctLinks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!knownUrls.Add(originUrl))
            {
                existing++;
                continue;
            }

            FirefightersArticle article;

            try
            {
                article = await firefightersArticlesRepository.GetArticleAsync(originUrl, cancellationToken);
                var result = await validator.ValidateAsync(article, cancellationToken);

                if (!result.IsValid)
                {
                    failed++;
                    logger.LogWarning("Skipping invalid Firefighters article {OriginUrl}: {Reasons}", originUrl, string.Join("; ", result.Errors.Select(error => error.ErrorMessage)));
                    continue;
                }
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                failed++;
                logger.LogError(exception, "Firefighters article detail failed for {OriginUrl}; continuing with other articles.", originUrl);
                continue;
            }

            cancellationToken.ThrowIfCancellationRequested();

            await articleRepository.AddAsync(new ArticleEntity
            {
                Title = article.Title,
                Content = article.Content,
                OriginUrl = originUrl,
                PublishedAt = article.PublishedAt,
                Type = ArticleType.Firefighters
            }, cancellationToken);

            imported++;
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (imported > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Firefighters article import finished: {Imported} imported, {Existing} existing, {Failed} failed.", imported, existing, failed);
    }
}
