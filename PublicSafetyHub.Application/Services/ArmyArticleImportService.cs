using FluentValidation;
using Microsoft.Extensions.Logging;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Base.UnitOfWork;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Core.Enums;
using PublicSafetyHub.Core.Repositories;
using PublicSafetyHub.Integration.Repositories.Interfaces;
using PublicSafetyHub.Integration.IntegrationModels;

namespace PublicSafetyHub.Application.Services;

public class ArmyArticleImportService(
    IArmyArticlesRepository armyArticlesRepository,
    IArticleRepository articleRepository,
    IUnitOfWork unitOfWork,
    IValidator<ArmyArticle> armyArticleValidator,
    ILogger<ArmyArticleImportService> logger)
    : IArmyArticleImportService
{
    public async Task ImportAsync()
    {
        var imported = 0;
        var existing = 0;
        var invalid = 0;

        var articles = await GetArticlesAsync();
        var validArticles = new List<ArmyArticle>();

        foreach (var article in articles)
        {
            var validationResult = await armyArticleValidator.ValidateAsync(article);

            if (!validationResult.IsValid)
            {
                invalid++;
                var reasons = string.Join("; ", validationResult.Errors.Select(error => error.ErrorMessage));
                logger.LogWarning("Skipping invalid Army article {OriginUrl}: {Reasons}", article.OriginUrl, reasons);
                continue;
            }

            validArticles.Add(article);
        }

        var originUrls = validArticles.Select(article => article.OriginUrl).ToArray();
        var existingOriginUrls = await articleRepository.GetExistingOriginUrlsAsync(originUrls);
        var knownOriginUrls = existingOriginUrls.ToHashSet(StringComparer.Ordinal);

        foreach (var article in validArticles)
        {
            if (!knownOriginUrls.Add(article.OriginUrl))
            {
                existing++;
                continue;
            }

            await articleRepository.AddAsync(new ArticleEntity
            {
                Title = article.Title,
                Content = article.Description,
                OriginUrl = article.OriginUrl,
                PublishedAt = article.PublishedAt,
                Type = ArticleType.Army
            });

            imported++;
        }

        if (imported > 0)
        {
            await unitOfWork.SaveChangesAsync();
        }

        logger.LogInformation("Army article import finished: {Imported} imported, {Existing} existing, {Invalid} invalid.", imported, existing, invalid);
    }

    public Task<IReadOnlyList<ArmyArticle>> GetArticlesAsync()
    {
        return armyArticlesRepository.GetArticlesAsync();
    }
}