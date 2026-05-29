using System.Text.RegularExpressions;

namespace Content.Shared._Orion.RichText;

public static class SafeMarkup
{
    private static readonly Regex MarkupTagRegex = new(@"(?<!\\)\[/?(?<tag>[a-zA-Z][a-zA-Z0-9-]*)(?:=[^\]\r\n]*)?/?\]", RegexOptions.Compiled);

    private static readonly string[] BasicMarkupTags =
    {
        "bolditalic",
        "bold",
        "bullet",
        "color",
        "head",
        "italic",
        "mono",
    };

    private static readonly string[] NewsArticleMarkupTags = BasicMarkupTags;

    public static string SanitizeBasic(string text)
    {
        return Sanitize(text, BasicMarkupTags);
    }

    public static string SanitizeNewsArticle(string text)
    {
        return Sanitize(text, NewsArticleMarkupTags);
    }

    private static string Sanitize(string text, IReadOnlyList<string> allowedTags)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return MarkupTagRegex.Replace(text,
            match =>
        {
            var tag = match.Groups["tag"].Value;
            return IsAllowedTag(tag, allowedTags)
                ? match.Value
                : string.Empty;
        });
    }

    private static bool IsAllowedTag(string tag, IReadOnlyList<string> allowedTags)
    {
        foreach (var allowedTag in allowedTags)
        {
            if (string.Equals(tag, allowedTag, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
