using FluentValidation;
using Microsoft.Extensions.Logging;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Base.UnitOfWork;
using PublicSafetyHub.Core.Configurations;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Core.Repositories;
using PublicSafetyHub.Integration.Repositories.Interfaces;
using PublicSafetyHub.Integration.IntegrationModels;
using PublicSafetyHub.Core.Enums;

namespace PublicSafetyHub.Application.Services;

public class FirefightersArticleImportService(
    IFirefightersArticlesRepository firefightersArticlesRepository,
    IArticleRepository articleRepository,
    IUnitOfWork unitOfWork,
    IValidator<FirefightersArticle> firefightersArticleValidator,
    IFirefightersArticleImportOptions firefightersArticleImportOptions,
    ILogger<FirefightersArticleImportService> logger) : IFirefightersArticleImportService
{
    public async Task ImportAsync()
    {
        if (string.IsNullOrWhiteSpace(firefightersArticleImportOptions.Username) || string.IsNullOrWhiteSpace(firefightersArticleImportOptions.Password))
        {
            logger.LogWarning("Firefighters article import skipped: IMAP credentials are missing.");
            return;
        }

        var links = await firefightersArticlesRepository.GetArticleLinksFromEmailNewslettersAsync();
        var distinctLinks = links.Distinct(StringComparer.Ordinal).ToArray();
        var existingUrls = await articleRepository.GetExistingOriginUrlsAsync(distinctLinks);

        var imported = 0;
        var existing = 0;
        var failed = 0;

        var knownUrls = existingUrls.ToHashSet(StringComparer.Ordinal);

        foreach (var originUrl in distinctLinks)
        {
            if (!knownUrls.Add(originUrl))
            {
                existing++;
                continue;
            }

            FirefightersArticle article;

            try
            {
                article = await firefightersArticlesRepository.GetArticleAsync(originUrl);
                var result = await firefightersArticleValidator.ValidateAsync(article);

                if (!result.IsValid)
                {
                    failed++;
                    logger.LogWarning("Skipping invalid Firefighters article {OriginUrl}: {Reasons}", originUrl, string.Join("; ", result.Errors.Select(error => error.ErrorMessage)));
                    continue;
                }
            }
            catch (Exception exception)
            {
                failed++;
                logger.LogError(exception, "Firefighters article detail failed for {OriginUrl}; continuing with other articles.", originUrl);
                continue;
            }

            await articleRepository.AddAsync(new ArticleEntity
            {
                Title = article.Title,
                Content = article.Content,
                OriginUrl = originUrl,
                PublishedAt = article.PublishedAt,
                Type = ArticleType.Firefighters
            });

            imported++;
        }

        if (imported > 0)
        {
            await unitOfWork.SaveChangesAsync();
        }

        logger.LogInformation("Firefighters article import finished: {Imported} imported, {Existing} existing, {Failed} failed.", imported, existing, failed);
    }
}
