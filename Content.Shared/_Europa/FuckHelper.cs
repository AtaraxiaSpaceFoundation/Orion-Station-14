using Robust.Shared.Utility;
using System.Text;

namespace Content.Shared._Europa;

public static class FuckHelper
{
    public static string RemoveMarkupSafe(string markup)
    {
        if (string.IsNullOrEmpty(markup))
            return markup;

        try
        {
            var escaped = EscapeSpecialCharacters(markup);
            return FormattedMessage.RemoveMarkupOrThrow(escaped);
        }
        catch
        {
            return RemoveMarkupAndEscapeFallback(markup);
        }
    }

    public static string EscapeSpecialCharacters(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var sb = new StringBuilder();

        foreach (var c in text)
        {
            switch (c)
            {
                case '\\':
                    sb.Append(@"\\");
                    break;
                case '[':
                    sb.Append(@"\[");
                    break;
                case ']':
                    sb.Append(@"\]");
                    break;
                case '{':
                    sb.Append(@"\{");
                    break;
                case '}':
                    sb.Append(@"\}");
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }

    private static string RemoveMarkupAndEscapeFallback(string markup)
    {
        var result = new StringBuilder();
        var inTag = false;
        var depth = 0;

        foreach (var c in markup)
        {
            if (c == '[' && !inTag)
            {
                inTag = true;
                depth = 1;
                continue;
            }

            if (c == ']' && inTag)
            {
                depth--;
                if (depth == 0)
                {
                    inTag = false;
                }
                continue;
            }

            if (c == '[' && inTag)
            {
                depth++;
                continue;
            }

            if (!inTag)
            {
                switch (c)
                {
                    case '\\':
                        result.Append(@"\\");
                        break;
                    case '[':
                        result.Append(@"\[");
                        break;
                    case ']':
                        result.Append(@"\]");
                        break;
                    default:
                        result.Append(c);
                        break;
                }
            }
        }

        return result.ToString();
    }

    public static string SanitizeSimpleMessageForChat(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        var withoutMarkup = RemoveMarkupSafe(message);
        return EscapeSpecialCharacters(withoutMarkup);
    }

    public static string RemoveAllMarkupSymbols(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var sb = new StringBuilder();

        foreach (var c in text)
        {
            // Пропускаем все специальные символы разметки
            if (c is '\\' or '[' or ']' or '{' or '}')
                continue;

            sb.Append(c);
        }

        return sb.ToString();
    }
}
