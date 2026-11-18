using System.Globalization;
using System.Net;
using HtmlAgilityPack;
using PublicSafetyHub.Core.Configurations;
using PublicSafetyHub.Core.Helpers;
using PublicSafetyHub.Models.Articles;
using PublicSafetyHub.Core.Repositories.Integration;
using PublicSafetyHub.Integration.Clients;

namespace PublicSafetyHub.Integration.Repositories;

public class PoliceArticlesRepository(IPoliceArticlesClient policeArticlesClient, IPoliceArticleImportOptions options) : IPoliceArticlesRepository
{
    public async Task<IReadOnlyList<PoliceArticleLink>> GetListAsync(int pageNumber)
    {
        using var response = await policeArticlesClient.GetListAsync(pageNumber);

        if (pageNumber > 1 && response.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }

        response.EnsureSuccessStatusCode();
        var document = new HtmlDocument();
        document.LoadHtml(await response.Content.ReadAsStringAsync());

        var main = document.DocumentNode.SelectSingleNode("//main[@id='main']");
        var heading = main?.SelectSingleNode(".//h1");

        if (heading == null || TextHelper.NormalizeText(heading.InnerText) != "Zpravodajství")
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

            return [];
        }

        var baseUrl = new Uri(options.BaseUrl);
        var links = new List<PoliceArticleLink>();

        foreach (var node in articleNodes)
        {
            var card = node.SelectSingleNode("ancestor::li[1]");
            var calendar = card?.SelectSingleNode(".//*[name()='svg' and contains(@class, 'lucide-calendar')]/..");
            DateOnly? publishedOn = DateOnly.TryParse(TextHelper.NormalizeText(calendar?.InnerText ?? ""),
                CultureInfo.GetCultureInfo("cs-CZ"), DateTimeStyles.AllowWhiteSpaces, out var date) ? date : null;

            var href = WebUtility.HtmlDecode(node.GetAttributeValue("href", "")).Trim();

            if (string.IsNullOrWhiteSpace(href) || !Uri.TryCreate(baseUrl, href, out var url))
            {
                throw new InvalidDataException($"Police list page {pageNumber} contains an invalid article URL.");
            }

            links.Add(new PoliceArticleLink
            {
                OriginUrl = new UriBuilder(url) { Fragment = "" }.Uri.AbsoluteUri,
                PublishedOn = publishedOn
            });
        }

        return links;
    }

    public async Task<PoliceArticle> GetArticleAsync(string originUrl)
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
            Title = TextHelper.NormalizeText(title.InnerText),
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
        return textNodes == null ? "" : TextHelper.NormalizeText(string.Join(" ", textNodes.Select(text => text.InnerText)));
    }
}
