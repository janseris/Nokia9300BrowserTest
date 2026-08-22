namespace OperaLegacyLab.Web.Infrastructure;

/// <summary>
/// Builds the small "page size / compression" notice PageSizeReportMiddleware injects at the top of
/// every page that uses the shared _Layout.cshtml. Marker is the literal placeholder _Layout.cshtml
/// emits right after &lt;body&gt; - the middleware replaces it with the real Render(...) output once
/// it knows this exact response's actual byte counts.
///
/// Reports more than just this response's own HTML body, on purpose: a real page load over the wire
/// also costs this response's own HTTP status-line-and-headers, whatever local &lt;img&gt; resources
/// the HTML references (each a separate request/response with its own headers), and the
/// GET /favicon.ico request essentially every browser fires automatically for every page whether or
/// not the page mentions one. Ignoring those and only counting the HTML body would understate what
/// this page really costs a phone on a slow link - which is the whole point of measuring in the first
/// place. The HTML body counts (uncompressed/gzip) are exact, measured straight from this response's
/// real bytes; the header and sub-resource-header figures are reasonable, clearly-marked (~)
/// estimates rather than a live self-request per resource just to find their exact size.
///
/// Plain HTML 3.2 table, no CSS, matching every other piece of chrome in this app.
/// </summary>
public static class PageSizeBanner
{
    public const string Marker = "<!--PAGE-SIZE-BANNER-->";

    public static string Render(
        int htmlBodyBytes, int htmlBodyGzipBytes,
        int responseHeaderBytes,
        int subresourceCount, int subresourceBytes,
        int faviconBytes,
        int totalUncompressedBytes, int totalWithGzipBodyBytes,
        string acceptEncodingHeader, bool requestedGzip, bool sentCompressed)
    {
        var acceptEncodingDisplay = string.IsNullOrEmpty(acceptEncodingHeader)
            ? "(not sent)"
            : Markup.Escape(acceptEncodingHeader);
        var savedPct = htmlBodyBytes > 0 ? 100.0 * (htmlBodyBytes - htmlBodyGzipBytes) / htmlBodyBytes : 0;

        string Kb(int bytes) => (bytes / 1024.0).ToString("0.0");

        return $"""
                <table border="1" cellpadding="3" cellspacing="0">
                <tr><td colspan="2"><font size="1"><b>Page weight / compression (real network cost of this page load)</b></font></td></tr>
                <tr><td><font size="1">HTML (this page, excluding this notice)</font></td><td><font size="1">{Kb(htmlBodyBytes)} KB uncompressed / {Kb(htmlBodyGzipBytes)} KB gzip ({savedPct:0}% smaller)</font></td></tr>
                <tr><td><font size="1">This response's own HTTP headers</font></td><td><font size="1">~{responseHeaderBytes} bytes</font></td></tr>
                <tr><td><font size="1">Local files this page auto-loads (images/CSS)</font></td><td><font size="1">{subresourceCount} file(s), ~{Kb(subresourceBytes)} KB (incl. their own headers)</font></td></tr>
                <tr><td><font size="1">Favicon (every browser requests this automatically)</font></td><td><font size="1">~{faviconBytes} bytes</font></td></tr>
                <tr><td><font size="1"><b>Total over the wire, this page load</b></font></td><td><font size="1"><b>~{Kb(totalUncompressedBytes)} KB uncompressed / ~{Kb(totalWithGzipBodyBytes)} KB with gzip body</b></font></td></tr>
                <tr><td><font size="1">Browser requested compression</font></td><td><font size="1">{(requestedGzip ? "yes" : "no")} (Accept-Encoding: {acceptEncodingDisplay})</font></td></tr>
                <tr><td><font size="1">This response sent compressed</font></td><td><font size="1">{(sentCompressed ? "yes (gzip)" : "no")}</font></td></tr>
                </table>
                """;
    }
}
