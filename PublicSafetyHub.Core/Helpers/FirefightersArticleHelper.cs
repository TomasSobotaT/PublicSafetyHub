using System.Net;
using HtmlAgilityPack;
using MimeKit;

namespace PublicSafetyHub.Core.Helpers;

public static class FirefightersArticleHelper
{
    public const string Sender = "webmaster@mv.gov.cz";
    public const string Subject = "Novinky z webu HZSČR";
    private static readonly string[] subscriptionLinks = ["nastavení odběru", "nastaveni odberu", "unsubscribe", "/subscription", "/newsletter", "odhlásit", "odhlasit"];
    private static readonly string[] contactPrefixes = ["HZS ", "Mobil:", "Telefon:", "Tel.:", "Tel:", "e-mail:", "email:"];

    public static async Task<IReadOnlyList<string>> ParseEmlAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var message = await MimeMessage.LoadAsync(stream, cancellationToken);
        return Parse(message);
    }

    public static IReadOnlyList<string> Parse(MimeMessage message)
    {
        if (!IsNewsletter(message))
        {
            return [];
        }

        return ParseHtml(message.HtmlBody);
    }

    public static IReadOnlyList<string> ParseHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            throw new InvalidDataException("Firefighters newsletter has no HTML body.");
        }

        var document = new HtmlDocument();
        document.LoadHtml(html);
        var links = new HashSet<string>(StringComparer.Ordinal);
        var anchors = document.DocumentNode.SelectNodes("//a[@href]");

        if (anchors is null)
        {
            return [];
        }

        foreach (var anchor in anchors)
        {
            var href = WebUtility.HtmlDecode(anchor.GetAttributeValue("href", "")).Trim();
            var label = HtmlHelper.NormalizeText(anchor.InnerText);

            if (IsSubscriptionLink(label) || IsSubscriptionLink(href))
            {
                continue;
            }

            var originUrl = NormalizeUrl(href);

            if (originUrl is null)
            {
                continue;
            }

            links.Add(originUrl);
        }

        return [.. links];
    }

    private static bool IsNewsletter(MimeMessage message)
    {
        return message.From.Mailboxes.Any(mailbox => string.Equals(mailbox.Address, Sender, StringComparison.OrdinalIgnoreCase)) && string.Equals(message.Subject, Subject, StringComparison.Ordinal);
    }

    private static bool IsSubscriptionLink(string value)
    {
        return subscriptionLinks.Any(part => value.Contains(part, StringComparison.OrdinalIgnoreCase));
    }

    public static string NormalizeUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || !string.Equals(uri.Host, "hzscr.gov.cz", StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrEmpty(uri.UserInfo)
            || !uri.IsDefaultPort)
        {
            return null;
        }

        return new UriBuilder(uri) { Fragment = "" }.Uri.AbsoluteUri;
    }

    public static string GetArticleContentText(HtmlNode content)
    {
        var articleContent = content.CloneNode(true);
        var signatureRole = articleContent.SelectNodes("./p")?.LastOrDefault(IsPressSpokesperson);

        if (signatureRole is not null && HasOnlyContactDetailsAfter(signatureRole))
        {
            var signatureStart = signatureRole;
            var previousParagraph = signatureRole.SelectSingleNode("preceding-sibling::*[1]");

            while (previousParagraph is not null && string.IsNullOrWhiteSpace(HtmlHelper.GetPlainText(previousParagraph)))
            {
                previousParagraph = previousParagraph.SelectSingleNode("preceding-sibling::*[1]");
            }

            var authorName = previousParagraph?.SelectSingleNode("./strong | ./b");

            if (authorName is not null
                && HtmlHelper.GetPlainText(previousParagraph) == HtmlHelper.GetPlainText(authorName))
            {
                signatureStart = previousParagraph;
            }

            while (signatureStart is not null)
            {
                var nextNode = signatureStart.NextSibling;
                signatureStart.Remove();
                signatureStart = nextNode;
            }
        }

        return HtmlHelper.GetPlainText(articleContent);
    }

    private static bool IsPressSpokesperson(HtmlNode paragraph)
    {
        var text = HtmlHelper.GetPlainText(paragraph);
        return string.Equals(text, "tisková mluvčí", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "tiskový mluvčí", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasOnlyContactDetailsAfter(HtmlNode signatureRole)
    {
        var followingElements = signatureRole.SelectNodes("following-sibling::*");
        return followingElements is null || followingElements.All(IsContactDetail);
    }

    private static bool IsContactDetail(HtmlNode node)
    {
        var text = HtmlHelper.GetPlainText(node);
        return string.IsNullOrWhiteSpace(text) || contactPrefixes.Any(prefix => text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}
