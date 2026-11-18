using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using HtmlAgilityPack;
using FluentValidation;
using Microsoft.Extensions.Logging;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Base.UnitOfWork;
using PublicSafetyHub.Core.Configurations;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Core.Enums;
using PublicSafetyHub.Data.Repositories.Interfaces;
using PublicSafetyHub.Integration.Army;
using PublicSafetyHub.Models.Articles;

namespace PublicSafetyHub.Application.Services;

public class ArmyArticleImportService(
    IArmyArticlesClient armyArticlesClient,
    IArmyArticleImportOptions options,
    IArticleRepository articleRepository,
    IUnitOfWork unitOfWork,
    IValidator<ArmyArticle> armyArticleValidator,
    ILogger<ArmyArticleImportService> logger)
    : IArmyArticleImportService
{
    public async Task ImportFeedAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var imported = 0;
        var existing = 0;
        var invalid = 0;

        var articles = await GetArticlesAsync();
        var validArticles = new List<ArmyArticle>();

        foreach (var article in articles)
        {
            cancellationToken.ThrowIfCancellationRequested();

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

        cancellationToken.ThrowIfCancellationRequested();

        var originUrls = validArticles.Select(article => article.OriginUrl).ToArray();
        var existingOriginUrls = await articleRepository.GetExistingOriginUrlsAsync(originUrls);
        var knownOriginUrls = existingOriginUrls.ToHashSet(StringComparer.Ordinal);

        foreach (var article in validArticles)
        {
            cancellationToken.ThrowIfCancellationRequested();

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

        cancellationToken.ThrowIfCancellationRequested();

        if (imported > 0)
        {
            await unitOfWork.SaveChangesAsync();
        }

        logger.LogInformation("Army article import finished: {Imported} imported, {Existing} existing, {Invalid} invalid.", imported, existing, invalid);
    }

    public async Task<IReadOnlyList<ArmyArticle>> GetArticlesAsync()
    {
        using var response = await armyArticlesClient.GetFeedAsync();
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var xmlReader = XmlReader.Create(stream, new XmlReaderSettings { Async = true, DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });

        var xDocument = await XDocument.LoadAsync(xmlReader, LoadOptions.None, CancellationToken.None);
        var channelXElement = xDocument.Element("rss")?.Element("channel") ?? throw new InvalidDataException("Army feed does not contain an RSS channel.");

        var baseUrl = new Uri(options.BaseUrl);
        var articles = new List<ArmyArticle>();
        var uniqueArticlesUrl = new HashSet<string>(StringComparer.Ordinal);

        foreach (var itemXElement in channelXElement.Elements("item"))
        {
            var linkXElementValue = itemXElement.Element("link")?.Value.Trim();

            var originUrl = linkXElementValue;

            if (!string.IsNullOrWhiteSpace(linkXElementValue) && Uri.TryCreate(baseUrl, linkXElementValue, out var url))
            {
                originUrl = new UriBuilder(url) { Fragment = "" }.Uri.AbsoluteUri;
            }

            if (string.IsNullOrWhiteSpace(originUrl) || uniqueArticlesUrl.Add(originUrl))
            {
                var description = new HtmlDocument();

                description.LoadHtml(itemXElement.Element("description")?.Value ?? "");

                articles.Add(new ArmyArticle
                {
                    Title = NormalizeText(itemXElement.Element("title")?.Value ?? ""),
                    Description = NormalizeText(description.DocumentNode.InnerText),
                    OriginUrl = originUrl,
                    PublishedAt = ParsePublishedAt(itemXElement.Element("pubDate")?.Value)
                });
            }
        }

        return articles;
    }

    private static DateTime? ParsePublishedAt(string value)
    {
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out var publishedAt))
        {
            return null;
        }

        return publishedAt.UtcDateTime;
    }

    private static string NormalizeText(string value)
    {
        return Regex.Replace(WebUtility.HtmlDecode(value), @"\s+", " ").Trim();
    }
}
