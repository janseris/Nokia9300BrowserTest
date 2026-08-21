using System.IO.Compression;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using OperaLegacyLab.Web.Infrastructure;

namespace OperaLegacyLab.Web.Pages.Test;

/// <summary>
/// Serves CompressionSample's HTML, actually gzip-compressed (System.IO.Compression.GZipStream, BCL
/// only - same "no extra dependency" property as PngQrEncoder), with Content-Encoding: gzip - sent
/// unconditionally regardless of whether this request's own Accept-Encoding advertised gzip support,
/// deliberately: the point of this resource is finding out what the browser can actually decompress,
/// not replaying back whatever it claimed to accept (a real production server should never do this;
/// this is a single-purpose diagnostic lab, not a pattern to copy elsewhere).
/// </summary>
public class CompressionGzipModel : LabPageModel
{
    public CompressionGzipModel(LabSessionStore store) : base(store) { }

    public IActionResult OnGet()
    {
        var lab = Store.Resolve(HttpContext);
        var raw = Encoding.ASCII.GetBytes(CompressionSample.Html("gzip", lab.Code));

        using var ms = new MemoryStream();
        using (var gzip = new GZipStream(ms, CompressionLevel.Optimal, leaveOpen: true))
            gzip.Write(raw);
        var compressed = ms.ToArray();

        Response.Headers.ContentEncoding = "gzip";
        Response.Headers.CacheControl = "no-store";
        return File(compressed, "text/html; charset=us-ascii");
    }
}
