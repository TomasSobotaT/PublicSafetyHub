using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using FluentValidation;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Base.UnitOfWork;
using PublicSafetyHub.Core.Configurations;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Core.Enums;
using PublicSafetyHub.Data.Repositories.Interfaces;
using PublicSafetyHub.Integration.Police;
using PublicSafetyHub.Models.Articles;

namespace PublicSafetyHub.Application.Services;

public class PoliceArticleImportService(
    IPoliceArticlesClient policeArticlesClient,
    IPoliceArticleImportOptions options,
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
        var links = await GetArticleLinksAsync(cutoff, cancellationToken);
        var originUrls = links.Select(link => link.OriginUrl).ToArray();
        var existingOriginUrls = await articleRepository.GetExistingOriginUrlsAsync(originUrls);
        var knownOriginUrls = existingOriginUrls.ToHashSet(StringComparer.Ordinal);
        var imported = 0;
        var existing = 0;
        var invalid = 0;
        var outsidePeriod = 0;

        foreach (var link in links)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!knownOriginUrls.Add(link.OriginUrl))
            {
                existing++;
                continue;
            }

            var article = await GetArticleAsync(link.OriginUrl);
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

        logger.LogInformation("Police article import finished since {Cutoff}: {Imported} imported, {Existing} existing, {Invalid} invalid, {OutsidePeriod} outside period.",
            cutoff, imported, existing, invalid, outsidePeriod);
    }

    private async Task<IReadOnlyList<PoliceArticleLink>> GetArticleLinksAsync(DateTime cutoff, CancellationToken cancellationToken)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Prague");
        var cutoffDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(cutoff, timeZone));
        var baseUrl = new Uri(options.BaseUrl);
        var links = new List<PoliceArticleLink>();
        var knownUrls = new HashSet<string>(StringComparer.Ordinal);
        var knownPages = new HashSet<string>(StringComparer.Ordinal);

        for (var pageNumber = 1; ; pageNumber = checked(pageNumber + 1))
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var response = await policeArticlesClient.GetListAsync(pageNumber);

            if (pageNumber > 1 && response.StatusCode == HttpStatusCode.NotFound)
            {
                break;
            }

            response.EnsureSuccessStatusCode();
            var document = new HtmlDocument();
            document.LoadHtml(await response.Content.ReadAsStringAsync());

            var main = document.DocumentNode.SelectSingleNode("//main[@id='main']");
            var heading = main?.SelectSingleNode(".//h1");

            if (heading == null || NormalizeText(heading.InnerText) != "Zpravodajství")
            {
                throw new InvalidDataException($"Police list page {pageNumber} has an unexpected structure.");
            }

            var articleNodes = main.SelectNodes(".//h3/a[@href]");

            if (articleNodes == null || articleNodes.Count == 0)
            {
                var emptyList = main.SelectSingleNode(".//ul[contains(concat(' ', normalize-space(@class), ' '), ' flex-col ') and contains(concat(' ', normalize-space(@class), ' '), ' gap-8 ') and contains(concat(' ', normalize-space(@class), ' '), ' list-none ')]");

                if (emptyList == null || emptyList.SelectSingleNode("./li") != null)
                {
                    throw new InvalidDataException($"Police list page {pageNumber} contains neither article links nor a recognized empty list.");
                }

                break;
            }

            var pageKey = string.Join("\n", articleNodes.Select(node => node.GetAttributeValue("href", "")));

            if (!knownPages.Add(pageKey))
            {
                logger.LogWarning("Police list repeated a previously visited page at page {PageNumber}; pagination stopped.", pageNumber);
                break;
            }

            var allOlder = true;
            var pageLinks = new List<PoliceArticleLink>();

            foreach (var node in articleNodes)
            {
                var card = node.SelectSingleNode("ancestor::li[1]");
                var calendar = card?.SelectSingleNode(".//*[name()='svg' and contains(@class, 'lucide-calendar')]/..");
                DateOnly? publishedOn = DateOnly.TryParse(NormalizeText(calendar?.InnerText ?? ""),
                    CultureInfo.GetCultureInfo("cs-CZ"), DateTimeStyles.AllowWhiteSpaces, out var date) ? date : null;

                if (!publishedOn.HasValue || publishedOn.Value >= cutoffDate)
                {
                    allOlder = false;
                }

                var href = WebUtility.HtmlDecode(node.GetAttributeValue("href", "")).Trim();

                if (string.IsNullOrWhiteSpace(href) || !Uri.TryCreate(baseUrl, href, out var url))
                {
                    throw new InvalidDataException($"Police list page {pageNumber} contains an invalid article URL.");
                }

                var originUrl = new UriBuilder(url) { Fragment = "" }.Uri.AbsoluteUri;

                if (knownUrls.Add(originUrl))
                {
                    pageLinks.Add(new PoliceArticleLink { OriginUrl = originUrl, PublishedOn = publishedOn });
                }
            }

            if (allOlder)
            {
                break;
            }

            links.AddRange(pageLinks);
        }

        return links;
    }

    private async Task<PoliceArticle> GetArticleAsync(string originUrl)
    {
        using var response = await policeArticlesClient.GetArticleAsync(originUrl);
        response.EnsureSuccessStatusCode();

        var document = new HtmlDocument();
        document.LoadHtml(await response.Content.ReadAsStringAsync());

        var title = document.DocumentNode.SelectSingleNode("//main[@id='main']//h1");
        var content = document.DocumentNode.SelectSingleNode("//main[@id='main']//section[@id='article-chapter-content']");

        if (title == null || content == null)
        {
            throw new InvalidDataException($"Police article {originUrl} has an unexpected structure.");
        }

        var perex = content.SelectSingleNode("preceding-sibling::p[1]");
        var publishedValue = document.DocumentNode.SelectSingleNode("//meta[@property='article:published_time']")?.GetAttributeValue("content", "");
        DateTime? publishedAt = DateTimeOffset.TryParse(publishedValue, CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out var published) ? published.UtcDateTime : null;

        var paragraphs = new List<string>();

        if (perex != null)
        {
            paragraphs.Add(GetPlainText(perex));
        }

        paragraphs.Add(GetPlainText(content));

        return new PoliceArticle
        {
            Title = NormalizeText(title.InnerText),
            Content = string.Join("\n\n", paragraphs.Where(paragraph => !string.IsNullOrWhiteSpace(paragraph))),
            OriginUrl = originUrl,
            PublishedAt = publishedAt
        };
    }

    private static string GetPlainText(HtmlNode node)
    {
        var clone = node.CloneNode(true);
        var excludedNodes = clone.SelectNodes(".//script | .//style | .//noscript");

        if (excludedNodes != null)
        {
            foreach (var excluded in excludedNodes)
            {
                excluded.Remove();
            }
        }

        var textNodes = clone.SelectNodes(".//text()");
        return textNodes == null ? "" : NormalizeText(string.Join(" ", textNodes.Select(text => text.InnerText)));
    }

    private static string NormalizeText(string value)
    {
        return Regex.Replace(WebUtility.HtmlDecode(value), @"\s+", " ").Trim();
    }
}
