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

        var document = await LoadDocumentAsync(response);

        var mainHtmlNode = document.DocumentNode.SelectSingleNode("//main[@id='main']");
        var headingHtmlNode = mainHtmlNode?.SelectSingleNode(".//h1");

        if (headingHtmlNode is null || HtmlHelper.NormalizeText(headingHtmlNode.InnerText) != "Zpravodajství")
        {
            throw new InvalidDataException($"Police list page {pageNumber} has an unexpected structure.");
        }

        var articleHtmlNodes = mainHtmlNode.SelectNodes(".//h3/a[@href]");

        if (articleHtmlNodes is null || articleHtmlNodes.Count == 0)
        {
            var emptyHtmlNodeList = mainHtmlNode.SelectSingleNode(".//ul[contains(concat(' ', normalize-space(@class), ' '), ' flex-col ') and contains(concat(' ', normalize-space(@class), ' '), ' gap-8 ') and contains(concat(' ', normalize-space(@class), ' '), ' list-none ')]");

            if (emptyHtmlNodeList is null || emptyHtmlNodeList.SelectSingleNode("./li") is not null)
            {
                throw new InvalidDataException($"Police list page {pageNumber} contains neither article links nor a recognized empty list.");
            }

            return [];
        }

        var baseUrl = new Uri(options.BaseUrl);
        var policeArticleLinks = new List<PoliceArticleLink>();

        foreach (var node in articleHtmlNodes)
        {
            var cardHtmlNode = node.SelectSingleNode("ancestor::li[1]");
            var calendarHtmlNode = cardHtmlNode?.SelectSingleNode(".//*[name()='svg' and contains(@class, 'lucide-calendar')]/..");
            DateOnly? publishedOn = DateOnly.TryParse(HtmlHelper.NormalizeText(calendarHtmlNode?.InnerText ?? ""), CultureInfo.GetCultureInfo("cs-CZ"), DateTimeStyles.AllowWhiteSpaces, out var date) ? date : null;

            var href = WebUtility.HtmlDecode(node.GetAttributeValue("href", "")).Trim();

            if (string.IsNullOrWhiteSpace(href) || !Uri.TryCreate(baseUrl, href, out var url))
            {
                throw new InvalidDataException($"Police list page {pageNumber} contains an invalid article URL.");
            }

            policeArticleLinks.Add(new PoliceArticleLink
            {
                OriginUrl = new UriBuilder(url) { Fragment = "" }.Uri.AbsoluteUri,
                PublishedOn = publishedOn
            });
        }

        return policeArticleLinks;
    }

    public async Task<PoliceArticle> GetArticleAsync(string originUrl)
    {
        using var response = await policeArticlesClient.GetArticleAsync(originUrl);

        var document = await LoadDocumentAsync(response);

        var titleHtmlNode = document.DocumentNode.SelectSingleNode("//main[@id='main']//h1");
        var contentHtmlNode = document.DocumentNode.SelectSingleNode("//main[@id='main']//section[@id='article-chapter-content']");

        if (titleHtmlNode is null || contentHtmlNode is null)
        {
            throw new InvalidDataException($"Police article {originUrl} has an unexpected structure.");
        }

        var perex = contentHtmlNode.SelectSingleNode("preceding-sibling::p[1]");
        var publishedValue = document.DocumentNode.SelectSingleNode("//meta[@property='article:published_time']")?.GetAttributeValue("content", "");
        DateTime? publishedAt = DateTimeOffset.TryParse(publishedValue, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out var published) ? published.UtcDateTime : null;

        var paragraphs = new List<string>();

        if (perex is not null)
        {
            paragraphs.Add(HtmlHelper.GetPlainText(perex));
        }

        paragraphs.Add(HtmlHelper.GetPlainText(contentHtmlNode));

        return new PoliceArticle
        {
            Title = HtmlHelper.NormalizeText(titleHtmlNode.InnerText),
            Content = string.Join("\n\n", paragraphs.Where(paragraph => !string.IsNullOrWhiteSpace(paragraph))),
            OriginUrl = originUrl,
            PublishedAt = publishedAt
        };
    }

    private static async Task<HtmlDocument> LoadDocumentAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        var document = new HtmlDocument();
        document.LoadHtml(responseContent);
        return document;
    }

}
