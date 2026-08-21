using System.IO.Compression;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using OperaLegacyLab.Web.Infrastructure;

namespace OperaLegacyLab.Web.Pages.Test;

/// <summary>
/// Serves CompressionSample's HTML compressed with System.IO.Compression.ZLibStream (BCL only) and
/// Content-Encoding: deflate. ZLibStream produces zlib-wrapped deflate data (RFC 1950 framing around
/// RFC 1951 deflate) - which is what RFC 2616 actually specifies the HTTP "deflate" token means, even
/// though (see CompressionDeflateRaw.cshtml.cs) plenty of real servers historically sent raw deflate
/// under the same name instead. This variant is the spec-correct one; the other is the common-bug one
/// - testing both is the only way to find out which (if either) this browser actually expects.
/// </summary>
public class CompressionDeflateModel : LabPageModel
{
    public CompressionDeflateModel(LabSessionStore store) : base(store) { }

    public IActionResult OnGet()
    {
        var lab = Store.Resolve(HttpContext);
        var raw = Encoding.ASCII.GetBytes(CompressionSample.Html("deflate (zlib-wrapped)", lab.Code));

        using var ms = new MemoryStream();
        using (var z = new ZLibStream(ms, CompressionLevel.Optimal, leaveOpen: true))
            z.Write(raw);
        var compressed = ms.ToArray();

        Response.Headers.ContentEncoding = "deflate";
        Response.Headers.CacheControl = "no-store";
        return File(compressed, "text/html; charset=us-ascii");
    }
}
