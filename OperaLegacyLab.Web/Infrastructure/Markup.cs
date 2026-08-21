using System.Net;

namespace OperaLegacyLab.Web.Infrastructure;

/// <summary>
/// A couple of small helpers still needed even with Razor doing most of the
/// HTML escaping/rendering: building "?lab=CODE" URLs, and HTML-escaping text
/// inside plain C# string-building code (e.g. the WML card, which isn't
/// Razor-rendered at all). Named "Markup" rather than "Html" specifically to
/// avoid colliding with the `Html` property every Razor Page already exposes.
/// </summary>
public static class Markup
{
    public static string Escape(string s) => WebUtility.HtmlEncode(s);

    /// <summary>Appends (or adds) the lab session code to a path as a query parameter.</summary>
    public static string U(string path, string labCode)
        => path.Contains('?') ? $"{path}&lab={labCode}" : $"{path}?lab={labCode}";
}
