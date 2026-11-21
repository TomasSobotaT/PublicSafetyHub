using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using HtmlAgilityPack;
using PublicSafetyHub.Core.Configurations;
using PublicSafetyHub.Integration.Helpers;
using PublicSafetyHub.Integration.IntegrationModels;
using PublicSafetyHub.Integration.Repositories.Interfaces;
using PublicSafetyHub.Integration.Clients;

namespace PublicSafetyHub.Integration.Repositories;

public class ArmyArticlesRepository(IArmyArticlesClient armyArticlesClient, IArmyArticleImportOptions options) : IArmyArticlesRepository
{
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
                    Title = HtmlHelper.NormalizeText(itemXElement.Element("title")?.Value ?? ""),
                    Description = HtmlHelper.NormalizeText(description.DocumentNode.InnerText),
                    OriginUrl = originUrl,
                    PublishedAt = ParsePublishedAt(itemXElement.Element("pubDate")?.Value)
                });
            }
        }

        return articles;
    }

    private static DateTime? ParsePublishedAt(string value)
    {
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out var publishedAt))
        {
            return null;
        }

        return publishedAt.UtcDateTime;
    }
}
