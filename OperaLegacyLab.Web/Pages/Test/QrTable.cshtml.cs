using Microsoft.AspNetCore.Mvc;
using OperaLegacyLab.Web.Infrastructure;
using QRCoder;

namespace OperaLegacyLab.Web.Pages.Test;

/// <summary>
/// Feasibility test for a ResidentPass-style rotating QR code, HTML-table-rendering variant. See
/// QrImage.cshtml.cs for the image variant this page was split from (both used to live together on
/// one combined /test/qr comparison page; this table version was found to render as a visibly
/// non-square rectangle on the real device - so the two are now independent tests with their own
/// self-report). Kept as its own page anyway (rather than deleted) since it's still useful evidence
/// for exactly how that distortion shows up.
///
/// The real cause (confirmed by measuring actual rendered cell size in headless Chromium, see
/// HtmlTableQrRenderer's own doc comment): a &lt;td&gt;'s height="N" attribute is only a minimum, and
/// the &amp;nbsp; every cell needs to avoid collapsing to nothing has a line box around 18-19px tall
/// at normal font size - about 3x taller than the declared 6px - regardless of colspan/width. Fixed
/// there via one style="font-size:1px;line-height:1px" attribute on the &lt;table&gt; element.
///
/// This checks whether the browser can show a ResidentPass-style rotating QR code rendered as an
/// HTML table (one cell per module, run-length encoded by row): a fresh, short-lived code every 15
/// seconds, scannable by someone else, with the secret itself never sent to the browser. Two things
/// this deliberately does NOT use, because earlier tests here found no confirmed AJAX/XMLHttpRequest
/// and no SVG support on Opera 6:
///   - The code is computed entirely on the server - the browser only ever receives the
///     already-rotated 6-digit code and the finished table markup, never the secret.
///   - The 15-second refresh is a plain &lt;meta http-equiv="refresh"&gt; full-page reload, no
///     JavaScript timer at all.
/// Unlike the image variant, this is plain HTML markup - not an image file, so there's no
/// image-compression format involved at all (whatever compression happens, if any, is only whatever
/// the HTTP layer itself applies, not anything this app does).
///
/// The page markup itself was deliberately trimmed to just the table plus one line of status text -
/// no explanatory prose, no heading - so there's minimal scrolling before the QR code is visible and
/// minimal bytes to transfer on a slow link. The paragraph above (and its counterpart in
/// QrImage.cshtml.cs) is where that explanation now lives instead.
/// </summary>
public class QrTableModel : LabPageModel
{
    private readonly DeviceResultLog _deviceLog;

    public QrTableModel(LabSessionStore store, DeviceResultLog deviceLog) : base(store) => _deviceLog = deviceLog;

    public string TableHtml { get; private set; } = "";
    public string Payload { get; private set; } = "";
    public string Code { get; private set; } = "";
    public int SecondsRemaining { get; private set; } = QrTestSettings.PeriodSeconds;
    public int TableSizePx { get; private set; }

    public void OnGet()
    {
        ResolveLab("QR code (HTML table) test");
        BuildQr();
    }

    public IActionResult OnPost()
    {
        ResolveLab("QR code (HTML table) test");
        var (result, note) = SelfReport.ReadForm(Request.Form);
        Lab.SelfReports["qr_table"] = result;
        Lab.SelfReportNotes["qr_table"] = note;
        _deviceLog.Record(HttpContext, "qr_table", "QR code (HTML table)", result, note);
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

        // Pick the largest whole pixels-per-module that still keeps the table's total height
        // (moduleCount * pixelsPerModule, it's always square) at or under TableMaxHeightPx, rather
        // than a fixed pixel size - so it stays reliably small on a short screen even if a longer
        // payload someday needs a bigger QR version (more modules).
        var moduleCount = data.ModuleMatrix.Count;
        var pixelsPerModule = Math.Max(1, QrTestSettings.TableMaxHeightPx / moduleCount);
        TableSizePx = moduleCount * pixelsPerModule;

        TableHtml = HtmlTableQrRenderer.Render(data, pixelsPerModule);
    }
}
