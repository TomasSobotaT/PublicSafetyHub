using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using HtmlAgilityPack;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Base.UnitOfWork;
using PublicSafetyHub.Core.Configurations;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Core.Enums;
using PublicSafetyHub.Data.Repositories.Interfaces;
using PublicSafetyHub.Integration.Army;
using PublicSafetyHub.Models.Articles;

namespace PublicSafetyHub.Application.Services;

public class ArmyArticleImportService(IArmyArticlesClient client, IArmyArticleImportOptions options, IArticleRepository articleRepository, IUnitOfWork unitOfWork) : IArmyArticleImportService
{
    public async Task<bool> ImportAsync(Uri originUrl)
    {
        var exists = await articleRepository.ExistsByOriginUrlAsync(originUrl.AbsoluteUri);
        if (exists)
        {
            return false;
        }

        var article = await GetArticleAsync(originUrl);
        await articleRepository.AddAsync(new ArticleEntity
        {
            Title = article.Title,
            Content = article.Content,
            OriginUrl = article.OriginUrl,
            Type = ArticleType.Army
        });
        await unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<Uri>> GetArticleUrlsAsync()
    {
        using var response = await client.GetFeedAsync();
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { Async = true, DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        var document = await XDocument.LoadAsync(reader, LoadOptions.None, CancellationToken.None);
        var channel = document.Element("rss")?.Element("channel")
            ?? throw new InvalidDataException("Army feed does not contain an RSS channel.");
        var baseUrl = new Uri(options.BaseUrl);
        var urls = new List<Uri>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var item in channel.Elements("item"))
        {
            var link = item.Element("link")?.Value.Trim();
            if (string.IsNullOrWhiteSpace(link) || !Uri.TryCreate(baseUrl, link, out var url) || !IsArticleUrl(url, baseUrl))
            {
                continue;
            }

            var canonicalUrl = new UriBuilder(url) { Fragment = "" }.Uri;
            if (seen.Add(canonicalUrl.AbsoluteUri))
            {
                urls.Add(canonicalUrl);
            }
        }

        return urls;
    }

    private async Task<ArmyArticle> GetArticleAsync(Uri originUrl)
    {
        ArgumentNullException.ThrowIfNull(originUrl);
        if (!IsArticleUrl(originUrl, new Uri(options.BaseUrl)))
        {
            throw new ArgumentException("Article URL must belong to the configured Army website.", nameof(originUrl));
        }

        using var response = await client.GetArticleAsync(originUrl);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        var document = new HtmlDocument();
        document.LoadHtml(html);
        var titleNode = document.DocumentNode.SelectSingleNode("//div[contains(concat(' ', normalize-space(@class), ' '), ' news-detail__header__content ')]//h1");
        var contentNode = document.DocumentNode.SelectSingleNode("//div[contains(concat(' ', normalize-space(@class), ' '), ' news-detail__content ')]");
        if (titleNode is null || contentNode is null)
        {
            throw new InvalidDataException($"Army article structure was not recognized: {originUrl}");
        }

        var excludedNodes = contentNode.SelectNodes(".//script|.//style|.//noscript|.//*[contains(concat(' ', normalize-space(@class), ' '), ' author ')]");
        if (excludedNodes is not null)
        {
            foreach (var node in excludedNodes.ToArray())
            {
                node.Remove();
            }
        }

        var text = new StringBuilder();
        AppendText(contentNode, text);
        var title = NormalizeText(titleNode.InnerText);
        var paragraphs = text.ToString().Split('\n').Select(NormalizeText).Where(line => line.Length > 0);
        var content = string.Join("\n\n", paragraphs);
        if (string.IsNullOrWhiteSpace(title) || title.Length > 400 || string.IsNullOrWhiteSpace(content) || originUrl.AbsoluteUri.Length > 2048)
        {
            throw new InvalidDataException($"Army article has empty or invalid title, content or URL: {originUrl}");
        }

        return new ArmyArticle { Title = title, Content = content, OriginUrl = originUrl.AbsoluteUri };
    }

    private static bool IsArticleUrl(Uri url, Uri baseUrl)
    {
        return url.IsAbsoluteUri && url.Scheme == Uri.UriSchemeHttps && url.IdnHost.Equals(baseUrl.IdnHost, StringComparison.OrdinalIgnoreCase)
            && url.Port == baseUrl.Port && string.IsNullOrEmpty(url.UserInfo);
    }

    private static string NormalizeText(string value)
    {
        return Regex.Replace(WebUtility.HtmlDecode(value), @"\s+", " ").Trim();
    }

    private static void AppendText(HtmlNode node, StringBuilder text)
    {
        if (node.NodeType == HtmlNodeType.Text)
        {
            text.Append(node.InnerText.Replace('\r', ' ').Replace('\n', ' '));
            return;
        }

        var block = node.Name is "p" or "div" or "h2" or "h3" or "h4" or "li" or "ul" or "ol" or "blockquote" or "br" or "tr";
        if (block)
        {
            text.Append('\n');
        }

        foreach (var child in node.ChildNodes)
        {
            AppendText(child, text);
        }

        if (block)
        {
            text.Append('\n');
        }
    }
}
