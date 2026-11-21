using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace PublicSafetyHub.Integration.Helpers;

public static class HtmlHelper
{
    public static string NormalizeText(string value)
    {
        return Regex.Replace(WebUtility.HtmlDecode(value), @"\s+", " ").Trim();
    }

    public static string GetPlainText(HtmlNode node)
    {
        var cloneHtmlNode = node.CloneNode(true);
        var excludedNodes = cloneHtmlNode.SelectNodes(".//script | .//style | .//noscript");

        if (excludedNodes is not null)
        {
            foreach (var excludedNode in excludedNodes)
            {
                excludedNode.Remove();
            }
        }

        var textNodes = cloneHtmlNode.SelectNodes(".//text()");
        return textNodes is null ? "" : NormalizeText(string.Join(" ", textNodes.Select(text => text.InnerText)));
    }
}
