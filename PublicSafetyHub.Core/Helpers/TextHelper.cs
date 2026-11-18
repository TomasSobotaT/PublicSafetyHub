using System.Net;
using System.Text.RegularExpressions;

namespace PublicSafetyHub.Core.Helpers;

public static class TextHelper
{
    public static string NormalizeText(string value)
    {
        return Regex.Replace(WebUtility.HtmlDecode(value), @"\s+", " ").Trim();
    }
}
