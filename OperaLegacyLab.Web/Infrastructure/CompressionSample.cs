namespace OperaLegacyLab.Web.Infrastructure;

/// <summary>
/// The tiny HTML page served (compressed) by each /test/compression/* resource - deliberately
/// identical content across the gzip/deflate/deflate-raw variants except for the one phrase naming
/// which Content-Encoding was used, so a side-by-side comparison isn't confused by anything else
/// differing between them. Plain US-ASCII only and no HTML entities beyond the literal characters
/// typed here, so a failure to read this text can only be blamed on compression, not on the separate
/// character-encoding question /test/encoding already covers.
///
/// Bypasses Razor's view engine entirely (each resource page returns this as raw compressed bytes via
/// File(), there's no .cshtml body to render) - same reason EncodingLatin1.cshtml.cs and
/// WapWml.cshtml.cs do the same: needing exact control over the literal bytes sent, which a
/// Content-Encoding test needs just as much as a character-encoding test does.
/// </summary>
public static class CompressionSample
{
    public static string Html(string variantLabel, string labCode)
    {
        var label = Markup.Escape(variantLabel);
        var code = Markup.Escape(labCode);
        return $"""
                <!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.01 Transitional//EN" "http://www.w3.org/TR/html4/loose.dtd">
                <html>
                <head><title>{label} compression sample</title></head>
                <body>
                <h1>If you can read this sentence clearly, {label} response decompression works.</h1>
                <p>This page's bytes on the wire were compressed with Content-Encoding: {label} - your
                browser had to decompress them itself before this could display at all.</p>
                <hr>
                <p>
                <a href="/test/compression?lab={code}">Back to compression test</a> |
                <a href="/report?lab={code}">View report</a>
                </p>
                <p><font size="1">Lab session: {code}</font></p>
                </body>
                </html>
                """;
    }
}
