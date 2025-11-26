using System.Linq;
using System.Text.RegularExpressions;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Client._Orion.StyleSheets;

//
// License-Identifier: GPL-3.0-or-later
//

/// <summary>
/// Extension methods for RichTextLabel to support safe BBCode formatting.
/// Only allows whitelisted tags to prevent client crashes.
/// </summary>
public static class RichTextExtensions
{
    private static readonly Regex TagRegex = new(@"\[/?(?<tag>[a-zA-Z]+)(?:=[^\\\]]+)?\]", RegexOptions.Compiled);

    private static readonly Type[] AllowedTags = new[]
    {
        typeof(BoldItalicTag), // [bolditalic]
        typeof(BoldTag), // [bold]
        typeof(ColorTag), // [color=Red] / [color=#FF0000]
        typeof(ItalicTag), // [italic]
    };

    /// <summary>
    /// Sanitizes the input string by removing unsupported BBCode tags (e.g. [font=...]), keeping only whitelisted ones.
    /// Prevents client crashes caused by malicious or malformed BBCode.
    /// </summary>
    /// <param name="text">Input text containing BBCode tags.</param>
    /// <returns>Text with only allowed tags remaining.</returns>
    public static string SanitizeMarkup(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return TagRegex.Replace(text,
            match =>
        {
            var tag = match.Groups["tag"].Value;
            return AllowedTags.Any(t => t.Name.Replace("Tag", "").Equals(tag, StringComparison.OrdinalIgnoreCase)) ? match.Value : "";
        });
    }

    /// <summary>
    /// Sets the text with support for safe BBCode formatting.
    /// All disallowed tags (e.g. [font=...]) are stripped out.
    /// </summary>
    /// <param name="label">Target RichTextLabel.</param>
    /// <param name="message">Message with BBCode markup.</param>
    public static void SetMarkup(this RichTextLabel label, string message)
    {
        var safeMessage = SanitizeMarkup(message);
        label.SetMessage(FormattedMessage.FromMarkupPermissive(safeMessage));
    }
}
