using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace OperaLegacyLab.Web.Infrastructure;

/// <summary>
/// Wraps every response coming out of the Razor Pages pipeline, buffers it in memory, and - only for
/// pages that use the shared _Layout.cshtml (detected by the literal PageSizeBanner.Marker comment
/// _Layout emits right after &lt;body&gt;, not by content-type sniffing) - replaces that marker with a
/// banner reporting this exact page's real HTML size (uncompressed and gzip), an estimate of this
/// response's own HTTP header bytes, the real file size of any local &lt;img&gt; / &lt;link&gt;
/// resources this page's HTML references (plus their own estimated header overhead), the implicit
/// favicon request every browser makes, a combined "total over the wire" figure, and whether this
/// request asked for/received gzip compression. See PageSizeBanner's own doc comment for why more than
/// just the HTML body is counted.
///
/// Detecting eligibility by marker rather than content-type matters because a few pages deliberately
/// opt OUT of the shared layout for their own byte-exact testing - EncodingLatin1.cshtml.cs (genuine
/// ISO-8859-1 bytes), WapWml.cshtml.cs (WML, not HTML), ReportText.cshtml.cs (plain text), the
/// /test/qr-png image resource, and every /test/compression/* resource (which sets its own
/// Content-Encoding deliberately, forcing a specific one regardless of what was asked for). None of
/// those contain the marker, so they pass through this middleware completely untouched - no banner,
/// no compression decision made here, no risk of this middleware corrupting a test whose entire point
/// is controlling the exact bytes on the wire. (The Content-Encoding-already-set check below catches
/// the /test/compression/* case even before attempting to decode the body as text, belt-and-braces.)
///
/// The compression decision made here is also functionally different from /test/compression/*: this
/// is real, conditional gzip compression for ordinary pages (deliberately absent everywhere else in
/// this app before now) - it only compresses when THIS specific request's Accept-Encoding actually
/// asked for gzip, the way a real server negotiates, rather than always forcing one encoding the way
/// the dedicated test does. That makes every ordinary page in the app a second, more realistic
/// compression data point alongside that dedicated forced test.
/// </summary>
public sealed class PageSizeReportMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _env;

    public PageSizeReportMiddleware(RequestDelegate next, IWebHostEnvironment env)
    {
        _next = next;
        _env = env;
    }

    // Matches both src="..." (img) and href="..." (Css2.cshtml's <link rel="stylesheet">) - anything
    // else a href might point to (e.g. this app's own "/report?lab=..." nav links) simply won't exist
    // as a physical file under wwwroot and gets filtered out by the File.Exists check below, so
    // widening the match to href is safe rather than double-counting or miscounting page navigation.
    private static readonly Regex LocalSrcPattern = new("(?:src|href)=\"(/[^\"]+)\"", RegexOptions.Compiled);

    // Every browser, this one included, requests GET /favicon.ico automatically for every page -
    // whether or not any HTML links to it. This app doesn't ship one (see wwwroot/), so that request
    // gets a plain 404 back - small, but real, and easy to miss entirely if only counting the page's
    // own HTML. Approximated rather than measured live: nothing here makes a self-request just to find
    // out one 404 response's exact byte count.
    private const int AssumedFavicon404Bytes = 200;

    // A typical StaticFileMiddleware response's own status-line-and-headers (Content-Type,
    // Content-Length, Last-Modified, ETag, Accept-Ranges, Cache-Control) runs to roughly this many
    // bytes - approximated per referenced sub-resource for the same reason: not worth a real
    // self-request per <img> just to measure it exactly.
    private const int AssumedStaticFileHeaderBytes = 180;

    public async Task InvokeAsync(HttpContext context)
    {
        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context);
        }
        finally
        {
            context.Response.Body = originalBody;
        }

        buffer.Seek(0, SeekOrigin.Begin);
        var rawBytes = buffer.ToArray();

        // Anything other than a plain 200, or a response that already set its own Content-Encoding
        // (every /test/compression/* resource does this deliberately) - leave completely alone.
        if (context.Response.StatusCode != StatusCodes.Status200OK ||
            context.Response.Headers.ContainsKey("Content-Encoding"))
        {
            await originalBody.WriteAsync(rawBytes);
            return;
        }

        // Razor's view engine always writes UTF-8 (see EncodingLatin1.cshtml.cs's own doc comment on
        // this exact fact), so decoding as UTF-8 is correct for anything that actually went through
        // _Layout.cshtml. For content that didn't (raw binary like /test/qr-png's PNG bytes, or a
        // different charset like EncodingLatin1's ISO-8859-1), this decode may produce a garbled
        // string - harmless, since it's used only to search for the marker below, and the ORIGINAL
        // untouched rawBytes (never this decoded/re-encoded string) are what get written out if the
        // marker isn't found.
        var rawHtml = Encoding.UTF8.GetString(rawBytes);
        var markerIndex = rawHtml.IndexOf(PageSizeBanner.Marker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            await originalBody.WriteAsync(rawBytes);
            return;
        }

        var withoutMarker = rawHtml.Replace(PageSizeBanner.Marker, "");
        var contentBytes = Encoding.UTF8.GetByteCount(withoutMarker);
        var gzipBytes = Gzip(Encoding.UTF8.GetBytes(withoutMarker)).Length;

        var acceptEncoding = context.Request.Headers.AcceptEncoding.ToString();
        var requestedGzip = acceptEncoding.Contains("gzip", StringComparison.OrdinalIgnoreCase);

        var (subresourceCount, subresourceBytes) = SumLocalSubresourceBytes(withoutMarker);
        var faviconBytes = LocalFaviconBytes();
        var responseHeaderBytes = EstimateResponseHeaderBytes(context, requestedGzip);

        var totalUncompressed = contentBytes + responseHeaderBytes + subresourceBytes + faviconBytes;
        var totalWithGzipBody = gzipBytes + responseHeaderBytes + subresourceBytes + faviconBytes;

        var banner = PageSizeBanner.Render(
            contentBytes, gzipBytes,
            responseHeaderBytes,
            subresourceCount, subresourceBytes,
            faviconBytes,
            totalUncompressed, totalWithGzipBody,
            acceptEncoding, requestedGzip, requestedGzip);

        var finalBytes = Encoding.UTF8.GetBytes(rawHtml.Replace(PageSizeBanner.Marker, banner));

        context.Response.Headers.Remove("Content-Length");

        if (requestedGzip)
        {
            var compressed = Gzip(finalBytes);
            context.Response.Headers.ContentEncoding = "gzip";
            context.Response.ContentLength = compressed.Length;
            await originalBody.WriteAsync(compressed);
        }
        else
        {
            context.Response.ContentLength = finalBytes.Length;
            await originalBody.WriteAsync(finalBytes);
        }
    }

    /// <summary>
    /// Scans the page's own rendered HTML for local (same-origin, path-only) src="..."/href="..."
    /// references - &lt;img src&gt; and Css2.cshtml's &lt;link rel="stylesheet" href&gt; are the only
    /// ones this app currently has - and sums each UNIQUE real file's on-disk size under wwwroot, plus
    /// the flat per-request header estimate. A resource referenced twice on one page (none currently
    /// are) is still only counted once, matching how a browser's own cache would behave within a single
    /// page load. A CSS file's own url(...) background-image references (Css2.cshtml has two) are NOT
    /// followed - they're a real, uncounted gap, documented rather than silently pretended away.
    /// </summary>
    private (int count, int bytes) SumLocalSubresourceBytes(string html)
    {
        // src="..."/href="..." also catches this app's own in-page navigation (e.g. the "Home" and
        // "View report" footer links, or a page linking to itself for a hover test) - those resolve to
        // Razor Page routes, not physical files, so they're deliberately excluded from BOTH the count
        // and the byte total by the File.Exists check below, not just the byte total. Without that, a
        // page with three internal nav links and zero real sub-resources would misreport "3 files"
        // that cost nothing.
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var realFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long total = 0;

        foreach (Match m in LocalSrcPattern.Matches(html))
        {
            var path = m.Groups[1].Value;
            if (!seen.Add(path)) continue;

            var relative = path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var diskPath = Path.Combine(_env.WebRootPath, relative);
            if (File.Exists(diskPath) && realFiles.Add(path))
                total += new FileInfo(diskPath).Length + AssumedStaticFileHeaderBytes;
        }

        return (realFiles.Count, (int)total);
    }

    private int LocalFaviconBytes()
    {
        var diskPath = Path.Combine(_env.WebRootPath, "favicon.ico");
        return File.Exists(diskPath)
            ? (int)new FileInfo(diskPath).Length + AssumedStaticFileHeaderBytes
            : AssumedFavicon404Bytes;
    }

    /// <summary>
    /// Builds this exact response's real status-line-and-headers the way HTTP/1.1 actually frames
    /// them (RFC 7230: status line, then "Name: Value\r\n" per header, then a blank line) and measures
    /// that - not a guessed round number. The one necessary approximation: Content-Length's own digit
    /// count depends on the final byte count, which isn't known until after the banner (built from
    /// this very number) is substituted in - a placeholder-width value is used instead of chasing that
    /// last one-or-two-byte circularity.
    /// </summary>
    private static int EstimateResponseHeaderBytes(HttpContext context, bool willBeGzipped)
    {
        var sb = new StringBuilder();
        sb.Append("HTTP/1.1 200 OK\r\n");
        foreach (var h in context.Response.Headers)
        {
            if (string.Equals(h.Key, "Content-Length", StringComparison.OrdinalIgnoreCase)) continue;
            sb.Append(h.Key).Append(": ").Append(h.Value).Append("\r\n");
        }
        if (willBeGzipped)
            sb.Append("Content-Encoding: gzip\r\n");
        sb.Append("Content-Length: 00000\r\n");
        sb.Append("\r\n");
        return Encoding.ASCII.GetByteCount(sb.ToString());
    }

    private static byte[] Gzip(byte[] data)
    {
        using var ms = new MemoryStream();
        using (var gzip = new GZipStream(ms, CompressionLevel.Optimal, leaveOpen: true))
            gzip.Write(data);
        return ms.ToArray();
    }
}
