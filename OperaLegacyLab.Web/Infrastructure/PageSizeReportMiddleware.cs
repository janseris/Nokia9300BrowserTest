using System.IO.Compression;
using System.Text;

namespace OperaLegacyLab.Web.Infrastructure;

/// <summary>
/// Wraps every response coming out of the Razor Pages pipeline, buffers it in memory, and - only for
/// pages that use the shared _Layout.cshtml (detected by the literal PageSizeBanner.Marker comment
/// _Layout emits right after &lt;body&gt;, not by content-type sniffing) - replaces that marker with a
/// banner reporting this exact page's real uncompressed size, its real gzip-compressed size, whether
/// this request's Accept-Encoding asked for gzip, and whether this response is actually being sent
/// compressed. All four numbers/facts are measured directly from THIS response, not estimated.
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

    public PageSizeReportMiddleware(RequestDelegate next) => _next = next;

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

        var banner = PageSizeBanner.Render(contentBytes, gzipBytes, acceptEncoding, requestedGzip, requestedGzip);
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

    private static byte[] Gzip(byte[] data)
    {
        using var ms = new MemoryStream();
        using (var gzip = new GZipStream(ms, CompressionLevel.Optimal, leaveOpen: true))
            gzip.Write(data);
        return ms.ToArray();
    }
}
