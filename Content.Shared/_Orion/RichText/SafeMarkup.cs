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

    private static readonly string[] NewsArticleMarkupTags =
    {
        "bolditalic",
        "bold",
        "bullet",
        "color",
        "head",
        "italic",
        "mono",
    };

    public static string SanitizeBasic(string text)
    {
        return Sanitize(text, BasicMarkupTags);
    }

    public static string SanitizeNewsArticle(string text)
    {
        return Sanitize(text, NewsArticleMarkupTags);
    }

    private static string Sanitize(string text, string[] allowedTags)
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

    private static bool IsAllowedTag(string tag, string[] allowedTags)
    {
        foreach (var allowedTag in allowedTags)
        {
            if (tag.Length != allowedTag.Length)
                continue;

            var matches = true;
            for (var i = 0; i < tag.Length; i++)
            {
                var c = tag[i];
                if (c is >= 'A' and <= 'Z')
                    c = (char) (c - 'A' + 'a');

                if (c == allowedTag[i])
                    continue;

                matches = false;
                break;
            }

            if (matches)
                return true;
        }

        return false;
    }
}
