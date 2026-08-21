using Microsoft.AspNetCore.Mvc;
using OperaLegacyLab.Web.Infrastructure;
using QRCoder;

namespace OperaLegacyLab.Web.Pages.Test;

/// <summary>
/// Serves the same TOTP QR code as /test/qr-image, but as a real PNG image (PngQrEncoder) rather than
/// an HTML table - see QrImage.cshtml.cs / QrTable.cshtml.cs and PngQrEncoder's own doc comment for
/// why: the table version turned out to render as a visibly non-square rectangle on the real device.
/// See HtmlTableQrRenderer's doc comment for the confirmed cause (a &lt;td&gt;'s declared height is
/// only a minimum; &amp;nbsp; at normal font size forced every row about 3x taller than declared) and
/// the fix applied there. A real image has no layout of its own for the browser to get wrong.
///
/// Independently computes "the TOTP code as of right now" rather than sharing state with the page
/// that embeds it - the same design every other page/resource pair in this app uses (see
/// DeviceResultLog's per-request model) - so there's a theoretical few-hundred-millisecond window
/// right at a 15-second boundary where this image's code could be one step ahead of the printed
/// "Code:" text on /test/qr-image. Harmless for a feasibility test; a production version would want
/// the two requests to agree, which is a separate problem from what's being tested here.
///
/// Named "qr-png" (not "qr-image") because /test/qr-image is now the full test page that embeds this
/// resource via an &lt;img&gt; tag - see the split from the original combined /test/qr comparison page.
/// </summary>
public class QrPngModel : LabPageModel
{
    public QrPngModel(LabSessionStore store) : base(store) { }

    public IActionResult OnGet(int px = QrTestSettings.PixelsPerModule)
    {
        var lab = Store.Resolve(HttpContext);

        var secret = Totp.GetOrCreateSecret(lab);
        var now = DateTimeOffset.UtcNow;
        var code = Totp.Compute(secret, now, QrTestSettings.PeriodSeconds);
        var payload = $"totp:{lab.Code}:{code}";

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.L);
        var png = PngQrEncoder.Encode(data, Math.Clamp(px, 2, 20));

        Response.Headers.CacheControl = "no-store";
        return File(png, "image/png");
    }
}
