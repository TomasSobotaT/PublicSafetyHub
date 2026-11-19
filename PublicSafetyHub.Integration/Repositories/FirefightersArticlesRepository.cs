using System.Globalization;
using HtmlAgilityPack;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using PublicSafetyHub.Core.Configurations;
using PublicSafetyHub.Core.Helpers;
using PublicSafetyHub.Core.Repositories.Integration;
using PublicSafetyHub.Integration.Clients;
using PublicSafetyHub.Models.Articles;

namespace PublicSafetyHub.Integration.Repositories;

public class FirefightersArticlesRepository(
    IFirefightersArticleImportOptions options,
    IFirefightersArticlesClient articlesClient) : IFirefightersArticlesRepository
{
    private const int ImapTimeoutMilliseconds = 60_000;

    public async Task<IReadOnlyList<string>> GetArticleLinksFromEmailNewslettersAsync(CancellationToken cancellationToken)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Prague");
        var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone).Date;
        var firstDate = today.AddDays(-1);
        var endDate = today.AddDays(1);
        var links = new HashSet<string>(StringComparer.Ordinal);

        using var client = new ImapClient();
        client.Timeout = ImapTimeoutMilliseconds;

        await client.ConnectAsync(options.ImapHost, options.ImapPort, SecureSocketOptions.SslOnConnect, cancellationToken);
        await client.AuthenticateAsync(options.Username, options.Password, cancellationToken);
        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

        var searchQuery = SearchQuery.DeliveredAfter(firstDate.AddDays(-1))
            .And(SearchQuery.DeliveredBefore(endDate.AddDays(1)))
            .And(SearchQuery.FromContains(FirefightersArticleHelper.Sender))
            .And(SearchQuery.SubjectContains(FirefightersArticleHelper.Subject));

        var newsletterMessageIds = await inbox.SearchAsync(searchQuery, cancellationToken);

        if (newsletterMessageIds.Count > 0)
        {
            var newsletterMessageSummaries = await inbox.FetchAsync(newsletterMessageIds, MessageSummaryItems.UniqueId | MessageSummaryItems.InternalDate, cancellationToken);

            foreach (var messageSummary in newsletterMessageSummaries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!messageSummary.InternalDate.HasValue)
                {
                    throw new InvalidDataException("Firefighters newsletter has no IMAP delivery date.");
                }

                var deliveredOn = TimeZoneInfo.ConvertTime(messageSummary.InternalDate.Value, timeZone).Date;

                if (deliveredOn >= firstDate && deliveredOn < endDate)
                {
                    using var message = await inbox.GetMessageAsync(messageSummary.UniqueId, cancellationToken);
                    links.UnionWith(FirefightersArticleHelper.Parse(message));
                }
            }
        }

        await client.DisconnectAsync(true, cancellationToken);
        return [.. links];
    }

    public async Task<FirefightersArticle> GetArticleAsync(string originUrl, CancellationToken cancellationToken)
    {
        var normalizedUrl = FirefightersArticleHelper.NormalizeUrl(originUrl) ?? throw new InvalidDataException("Firefighters article URL must use HTTPS on hzscr.gov.cz.");

        using var response = await articlesClient.GetArticleAsync(normalizedUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseArticle(html, normalizedUrl);
    }

    private static FirefightersArticle ParseArticle(string html, string originUrl)
    {
        var document = new HtmlDocument();
        document.LoadHtml(html);

        var title = document.DocumentNode.SelectSingleNode("//main//h1");
        var content = document.DocumentNode.SelectSingleNode("//main//section[@id='article-chapter-content']");
        var publishedValue = document.DocumentNode.SelectSingleNode("//meta[@property='article:published_time']")?.GetAttributeValue("content", "");

        var publishedAt = ValidateArticleAndGetPublishedAt(title, content, publishedValue, originUrl);

        var paragraphs = new List<string>();
        var perex = content.SelectSingleNode("preceding-sibling::p[1]");

        if (perex is not null)
        {
            var perexText = HtmlHelper.GetPlainText(perex);
            paragraphs.Add(perexText);
        }

        var contentText = FirefightersArticleHelper.GetArticleContentText(content);
        paragraphs.Add(contentText);

        return new FirefightersArticle
        {
            Title = HtmlHelper.NormalizeText(title.InnerText),
            Content = string.Join("\n\n", paragraphs.Where(paragraph => !string.IsNullOrWhiteSpace(paragraph))),
            OriginUrl = originUrl,
            PublishedAt = publishedAt
        };
    }

    private static DateTime ValidateArticleAndGetPublishedAt(HtmlNode title, HtmlNode content, string publishedValue, string originUrl)
    {
        if (title is null || content is null || !DateTimeOffset.TryParse(publishedValue, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out var published))
        {
            throw new InvalidDataException($"Firefighters article {originUrl} has an unexpected structure or publication date.");
        }

        return published.UtcDateTime;
    }
}
