using Microsoft.AspNetCore.Mvc;
using OperaLegacyLab.Web.Infrastructure;
using QRCoder;

namespace OperaLegacyLab.Web.Pages.Test;

/// <summary>
/// Feasibility test for a ResidentPass-style rotating QR code, image-rendering variant. See
/// QrTable.cshtml.cs for the HTML-table variant this page was split from (both used to live together
/// on one combined /test/qr comparison page; the table version was found to render as a visibly
/// non-square rectangle on the real device - see HtmlTableQrRenderer's doc comment for the confirmed
/// cause and fix - so the two are now independent tests with their own self-report so each can be
/// re-run and re-scored on its own).
///
/// This checks whether the browser can show a ResidentPass-style rotating QR code as a real PNG
/// image: a fresh, short-lived code every 15 seconds, scannable by someone else, with the secret
/// itself never sent to the browser. Two things this deliberately does NOT use, because earlier
/// tests here found no confirmed AJAX/XMLHttpRequest and no SVG support on Opera 6:
///   - The code is computed entirely on the server - the browser only ever receives the
///     already-rotated 6-digit code and the finished QR image, never the secret.
///   - The 15-second refresh is a plain &lt;meta http-equiv="refresh"&gt; full-page reload, no
///     JavaScript timer at all.
/// The actual QR pattern is a real PNG image (zlib/DEFLATE-compressed, via PngQrEncoder/ZLibStream),
/// fetched via an ordinary &lt;img&gt; tag from /test/qr-png (QrPngModel) - no table layout involved
/// at all.
///
/// The page markup itself was deliberately trimmed to just the image plus one line of status text -
/// no explanatory prose, no heading - so there's minimal scrolling before the QR code is visible and
/// minimal bytes to transfer on a slow link. The paragraph above (and its counterpart in
/// QrTable.cshtml.cs) is where that explanation now lives instead.
/// </summary>
public class QrImageModel : LabPageModel
{
    private readonly DeviceResultLog _deviceLog;

    public QrImageModel(LabSessionStore store, DeviceResultLog deviceLog) : base(store) => _deviceLog = deviceLog;

    public string Payload { get; private set; } = "";
    public string Code { get; private set; } = "";
    public int SecondsRemaining { get; private set; } = QrTestSettings.PeriodSeconds;
    public int ImageSize { get; private set; }

    public void OnGet()
    {
        ResolveLab("QR code (image) test");
        BuildQr();
    }

    public IActionResult OnPost()
    {
        ResolveLab("QR code (image) test");
        var (result, note) = SelfReport.ReadForm(Request.Form);
        Lab.SelfReports["qr_image"] = result;
        Lab.SelfReportNotes["qr_image"] = note;
        _deviceLog.Record(HttpContext, "qr_image", "QR code (image)", result, note);
        return Redirect(Markup.U("/report", Lab.Code));
    }

    private void BuildQr()
    {
        var secret = Totp.GetOrCreateSecret(Lab);
        var now = DateTimeOffset.UtcNow;
        Code = Totp.Compute(secret, now, QrTestSettings.PeriodSeconds);
        SecondsRemaining = Math.Max(1, Totp.SecondsRemaining(now, QrTestSettings.PeriodSeconds));
        Payload = $"totp:{Lab.Code}:{Code}";

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(Payload, QRCodeGenerator.ECCLevel.L);
        ImageSize = data.ModuleMatrix.Count * QrTestSettings.PixelsPerModule;
    }
}
