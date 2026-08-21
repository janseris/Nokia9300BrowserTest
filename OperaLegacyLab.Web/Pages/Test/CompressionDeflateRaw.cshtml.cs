using System.IO.Compression;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using OperaLegacyLab.Web.Infrastructure;

namespace OperaLegacyLab.Web.Pages.Test;

/// <summary>
/// Serves CompressionSample's HTML compressed with System.IO.Compression.DeflateStream (BCL only),
/// which produces RAW deflate data (RFC 1951) with no zlib wrapper at all - and labels it
/// Content-Encoding: deflate, the exact same header value CompressionDeflate.cshtml.cs sends for its
/// zlib-wrapped bytes. That's deliberate, not a bug here: real servers from this app's target era
/// famously disagreed about which of these two byte formats "deflate" meant (Apache and IIS both had
/// long-lived, widely-documented raw-vs-zlib mixups), so browsers of the time had to cope with
/// whichever one they actually got. Only testing both under the same header name reveals which one (if
/// either) this specific browser's decompressor actually expects.
/// </summary>
public class CompressionDeflateRawModel : LabPageModel
{
    public CompressionDeflateRawModel(LabSessionStore store) : base(store) { }

    public IActionResult OnGet()
    {
        var lab = Store.Resolve(HttpContext);
        var raw = Encoding.ASCII.GetBytes(CompressionSample.Html("deflate (raw, non-zlib)", lab.Code));

        using var ms = new MemoryStream();
        using (var d = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true))
            d.Write(raw);
        var compressed = ms.ToArray();

        Response.Headers.ContentEncoding = "deflate";
        Response.Headers.CacheControl = "no-store";
        return File(compressed, "text/html; charset=us-ascii");
    }
}
