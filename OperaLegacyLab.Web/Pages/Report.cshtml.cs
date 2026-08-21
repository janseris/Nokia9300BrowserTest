using OperaLegacyLab.Web.Infrastructure;

namespace OperaLegacyLab.Web.Pages;

public class ReportModel : LabPageModel
{
    public static readonly (string Key, string Label)[] SelfReportTests =
    {
        ("html", "HTML rendering"),
        ("css", "CSS support"),
        ("css2", "Extended CSS capabilities"),
        ("js", "JavaScript [1]-[14]"),
        ("js_dialog_alert", "JS alert()"),
        ("js_dialog_confirm", "JS confirm()"),
        ("js_dialog_prompt", "JS prompt()"),
        ("js2", "Extended JavaScript capabilities"),
        ("encoding", "Character encoding"),
        ("compression", "HTTP compression (gzip/deflate)"),
        ("ssr", "Small-screen reflow"),
        ("frames", "Frames"),
        ("qr_image", "QR code (image)"),
        ("qr_table", "QR code (HTML table)"),
    };

    public ReportModel(LabSessionStore store) : base(store) { }

    public void OnGet() => ResolveLab("Report");

    public string SelfReportResult(string key) =>
        Lab.SelfReports.TryGetValue(key, out var r) ? r : "(not yet tested)";

    public string SelfReportNote(string key) =>
        Lab.SelfReportNotes.TryGetValue(key, out var n) ? n : "";

    public string CookieResultText => Lab.CookieRoundTripPassed switch
    {
        true => "pass - cookie round-tripped",
        false => "fail - cookie did not come back",
        null => "(not yet tested)",
    };

    public string FormResultText => Lab.FormPosted
        ? "submitted - see field values below"
        : "(not yet submitted)";

    public string UaProfSummary => string.IsNullOrEmpty(Lab.WapProfileUrl)
        ? "not advertised by this browser"
        : $"{Lab.WapProfileUrl} ({Lab.WapProfileFetchStatus ?? "not fetched yet"})";
}
