using FluentValidation;
using Microsoft.Extensions.Logging;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Base.UnitOfWork;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Models.Enums;
using PublicSafetyHub.Core.Repositories;
using PublicSafetyHub.Core.Repositories.Integration;
using PublicSafetyHub.Models.Articles;

namespace PublicSafetyHub.Application.Services;

public class PoliceArticleImportService(
    IPoliceArticlesRepository policeArticlesRepository,
    IArticleRepository articleRepository,
    IUnitOfWork unitOfWork,
    IValidator<PoliceArticle> policeArticleValidator,
    ILogger<PoliceArticleImportService> logger)
    : IPoliceArticleImportService
{
    public async Task ImportAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var utcNow = DateTime.UtcNow;
        var latestPublishedAt = await articleRepository.GetLatestPublishedAtAsync(ArticleType.Police);
        var cutoff = latestPublishedAt ?? utcNow.AddDays(-2);
        var policeArticleLinks = await GetArticleLinksAsync(cutoff, cancellationToken);

        var originUrls = policeArticleLinks.Select(link => link.OriginUrl).ToArray();
        var existingOriginUrls = await articleRepository.GetExistingOriginUrlsAsync(originUrls);
        var knownOriginUrls = existingOriginUrls.ToHashSet(StringComparer.Ordinal);
        var imported = 0;
        var existing = 0;
        var invalid = 0;
        var outsidePeriod = 0;

        foreach (var policeArticleLink in policeArticleLinks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!knownOriginUrls.Add(policeArticleLink.OriginUrl))
            {
                existing++;
                continue;
            }

            var article = await policeArticlesRepository.GetArticleAsync(policeArticleLink.OriginUrl);
            var validationResult = await policeArticleValidator.ValidateAsync(article);

            if (!validationResult.IsValid)
            {
                invalid++;
                var reasons = string.Join("; ", validationResult.Errors.Select(error => error.ErrorMessage));
                logger.LogWarning("Skipping invalid Police article {OriginUrl}: {Reasons}", article.OriginUrl, reasons);
                continue;
            }

            if (article.PublishedAt < cutoff || article.PublishedAt > utcNow)
            {
                outsidePeriod++;
                continue;
            }

            await articleRepository.AddAsync(new ArticleEntity
            {
                Title = article.Title,
                Content = article.Content,
                OriginUrl = article.OriginUrl,
                PublishedAt = article.PublishedAt,
                Type = ArticleType.Police
            });

            imported++;
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (imported > 0)
        {
            await unitOfWork.SaveChangesAsync();
        }

        logger.LogInformation("Police article import finished since {Cutoff}: {Imported} imported, {Existing} existing, {Invalid} invalid, {OutsidePeriod} outside period.", cutoff, imported, existing, invalid, outsidePeriod);
    }

    private async Task<IReadOnlyList<PoliceArticleLink>> GetArticleLinksAsync(DateTime cutoff, CancellationToken cancellationToken)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Prague");
        var cutoffDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(cutoff, timeZone));
        var knownUrls = new HashSet<string>(StringComparer.Ordinal);
        var knownPages = new HashSet<string>(StringComparer.Ordinal);
        var policeArticleLinks = new List<PoliceArticleLink>();

        for (var pageNumber = 1; ; pageNumber = pageNumber + 1)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pageLinks = await policeArticlesRepository.GetListAsync(pageNumber);

            if (pageLinks.Count == 0)
            {
                break;
            }

            var pageKey = string.Join("\n", pageLinks.Select(link => link.OriginUrl));

            if (!knownPages.Add(pageKey))
            {
                logger.LogWarning("Police list repeated a previously visited page at page {PageNumber}; pagination stopped.", pageNumber);
                break;
            }

            if (pageLinks.All(link => link.PublishedOn.HasValue && link.PublishedOn.Value < cutoffDate))
            {
                break;
            }

            foreach (var pageLink in pageLinks)
            {
                if (knownUrls.Add(pageLink.OriginUrl))
                {
                    policeArticleLinks.Add(pageLink);
                }
            }
        }

        return policeArticleLinks;
    }
}
