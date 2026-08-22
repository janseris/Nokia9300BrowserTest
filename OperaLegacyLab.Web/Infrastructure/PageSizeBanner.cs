namespace OperaLegacyLab.Web.Infrastructure;

/// <summary>
/// Builds the small "page size / compression" notice PageSizeReportMiddleware injects at the top of
/// every page that uses the shared _Layout.cshtml. Marker is the literal placeholder _Layout.cshtml
/// emits right after &lt;body&gt; - the middleware replaces it with the real Render(...) output once
/// it knows this exact response's actual byte counts. Plain HTML 3.2 table, no CSS, matching every
/// other piece of chrome in this app.
/// </summary>
public static class PageSizeBanner
{
    public const string Marker = "<!--PAGE-SIZE-BANNER-->";

    /// <param name="uncompressedBytes">
    /// This exact page's real rendered size, measured directly from its own bytes - NOT counting this
    /// banner itself (the banner is this app's own instrumentation overhead, not "the page"), so the
    /// number shown is what this page would weigh if this feature didn't exist.
    /// </param>
    /// <param name="gzipBytes">
    /// Real gzip-compressed size of that same content (also excluding the banner), from actually
    /// running it through GZipStream - not an estimate.
    /// </param>
    /// <param name="acceptEncodingHeader">This request's own Accept-Encoding header, verbatim.</param>
    /// <param name="requestedGzip">Whether that header's value included "gzip".</param>
    /// <param name="sentCompressed">
    /// Whether THIS response is actually being sent gzip-compressed - see PageSizeReportMiddleware:
    /// unlike /test/compression/*, which always forces a specific encoding regardless of what was
    /// asked for (to separate "what it claims to accept" from "what it can decode"), ordinary pages
    /// only compress when this exact request's Accept-Encoding actually asked for it - closer to how a
    /// real server negotiates, and a second, more realistic data point.
    /// </param>
    public static string Render(int uncompressedBytes, int gzipBytes, string acceptEncodingHeader,
        bool requestedGzip, bool sentCompressed)
    {
        var uncompressedKb = uncompressedBytes / 1024.0;
        var gzipKb = gzipBytes / 1024.0;
        var savedPct = uncompressedBytes > 0 ? 100.0 * (uncompressedBytes - gzipBytes) / uncompressedBytes : 0;
        var acceptEncodingDisplay = string.IsNullOrEmpty(acceptEncodingHeader)
            ? "(not sent)"
            : Markup.Escape(acceptEncodingHeader);

        return $"""
                <table border="1" cellpadding="3" cellspacing="0">
                <tr><td colspan="2"><font size="1"><b>Page size / compression (this exact page, excluding this notice)</b></font></td></tr>
                <tr><td><font size="1">Uncompressed</font></td><td><font size="1">{uncompressedKb:0.0} KB</font></td></tr>
                <tr><td><font size="1">Gzip-compressed</font></td><td><font size="1">{gzipKb:0.0} KB ({savedPct:0}% smaller)</font></td></tr>
                <tr><td><font size="1">Browser requested compression</font></td><td><font size="1">{(requestedGzip ? "yes" : "no")} (Accept-Encoding: {acceptEncodingDisplay})</font></td></tr>
                <tr><td><font size="1">This response sent compressed</font></td><td><font size="1">{(sentCompressed ? "yes (gzip)" : "no")}</font></td></tr>
                </table>
                """;
    }
}
