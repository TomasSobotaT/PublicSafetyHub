using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using HtmlAgilityPack;
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
    ILogger<ArmyArticleImportService> logger)
    : IArmyArticleImportService
{
    public async Task ImportFeedAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var imported = 0;
        var skipped = 0;

        var articles = await GetArticlesAsync();

        foreach (var article in articles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var articleImported = await ImportAsync(article);

            if (articleImported)
            {
                imported++;
            }
            else
            {
                skipped++;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (imported > 0)
        {
            await unitOfWork.SaveChangesAsync();
        }

        logger.LogInformation("Army article import finished: {Imported} imported, {Skipped} existing.", imported, skipped);
    }

    public async Task<bool> ImportAsync(ArmyArticle article)
    {
        ArgumentNullException.ThrowIfNull(article);

        var originUrl = new Uri(article.OriginUrl, UriKind.Absolute);
        var articleUrlExists = await articleRepository.ExistsByOriginUrlAsync(originUrl.AbsoluteUri);

        if (articleUrlExists)
        {
            return false;
        }

        await articleRepository.AddAsync(new ArticleEntity
        {
            Title = article.Title,
            Content = article.Description,
            OriginUrl = originUrl.AbsoluteUri,
            Type = ArticleType.Army
        });

        return true;
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

            if (string.IsNullOrWhiteSpace(linkXElementValue) || !Uri.TryCreate(baseUrl, linkXElementValue, out var url))
            {
                continue;
            }

            var canonicalUrl = new UriBuilder(url) { Fragment = "" }.Uri;

            if (uniqueArticlesUrl.Add(canonicalUrl.AbsoluteUri))
            {
                var description = new HtmlDocument();

                description.LoadHtml(itemXElement.Element("description")?.Value ?? "");

                articles.Add(new ArmyArticle
                {
                    Title = NormalizeText(itemXElement.Element("title")?.Value ?? ""),
                    Description = NormalizeText(description.DocumentNode.InnerText),
                    OriginUrl = canonicalUrl.AbsoluteUri
                });
            }
        }

        return articles;
    }

    private static string NormalizeText(string value)
    {
        return Regex.Replace(WebUtility.HtmlDecode(value), @"\s+", " ").Trim();
    }
}
