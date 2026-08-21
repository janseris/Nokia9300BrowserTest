using Microsoft.AspNetCore.Mvc;
using OperaLegacyLab.Web.Infrastructure;
using QRCoder;

namespace OperaLegacyLab.Web.Pages.Test;

/// <summary>
/// Feasibility test for a ResidentPass-style rotating QR code (see the
/// project's ResidentPass.MAUI reference app, which regenerates a TOTP QR
/// code every 15 seconds while its BlazorWebView sits open). Opera 6 has no
/// confirmed working XMLHttpRequest/AJAX (see /test/js2 [13]-[14]) and no SVG
/// support, so this deliberately avoids both: the code is computed entirely
/// server-side, and the 15-second refresh is driven by a plain HTML
/// &lt;meta http-equiv="refresh"&gt; - a full-page reload, not a partial
/// update - since that's the only "make something change automatically"
/// mechanism this app can rely on existing here already (no JS timer needed
/// at all).
///
/// Two different ways of getting the actual QR pattern onto the page are
/// shown side by side: a real PNG image (PngQrEncoder/QrImage.cshtml.cs) and
/// an HTML table (HtmlTableQrRenderer, one cell per module). The table
/// version was tried first and, on the real device, rendered as a visibly
/// non-square rectangle - see PngQrEncoder's own doc comment for why the
/// image version was added as a very likely fix, and Qr.cshtml for why both
/// are still shown together rather than just replacing one with the other.
/// </summary>
public class QrModel : LabPageModel
{
    private readonly DeviceResultLog _deviceLog;

    public const int PeriodSeconds = 15;

    public QrModel(LabSessionStore store, DeviceResultLog deviceLog) : base(store) => _deviceLog = deviceLog;

    public const int PixelsPerModule = 6;

    public string TableHtml { get; private set; } = "";
    public string Payload { get; private set; } = "";
    public string Code { get; private set; } = "";
    public int SecondsRemaining { get; private set; } = PeriodSeconds;
    public int ImageSize { get; private set; }

    public void OnGet()
    {
        ResolveLab("QR code auto-refresh (TOTP) test");
        BuildQr();
    }

    public IActionResult OnPost()
    {
        ResolveLab("QR code auto-refresh (TOTP) test");
        var (result, note) = SelfReport.ReadForm(Request.Form);
        Lab.SelfReports["qr"] = result;
        Lab.SelfReportNotes["qr"] = note;
        _deviceLog.Record(HttpContext, "qr", "QR code auto-refresh (TOTP)", result, note);
        return Redirect(Markup.U("/report", Lab.Code));
    }

    private void BuildQr()
    {
        var secret = Totp.GetOrCreateSecret(Lab);
        var now = DateTimeOffset.UtcNow;
        Code = Totp.Compute(secret, now, PeriodSeconds);
        SecondsRemaining = Math.Max(1, Totp.SecondsRemaining(now, PeriodSeconds));
        Payload = $"totp:{Lab.Code}:{Code}";

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(Payload, QRCodeGenerator.ECCLevel.L);
        TableHtml = HtmlTableQrRenderer.Render(data, PixelsPerModule);
        ImageSize = data.ModuleMatrix.Count * PixelsPerModule;
    }
}
