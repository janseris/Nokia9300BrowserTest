using System.Text;
using Microsoft.AspNetCore.Mvc;
using OperaLegacyLab.Web.Infrastructure;

namespace OperaLegacyLab.Web.Pages;

public class ReportTextModel : LabPageModel
{
    public ReportTextModel(LabSessionStore store) : base(store) { }

    // Bypasses Razor rendering the same way EncodingLatin1/WapWml do - here
    // because the output is plain text, not because of byte-exact encoding
    // concerns (this is UTF-8, same as Razor's own output encoding).
    public IActionResult OnGet()
    {
        ResolveLab("Report (text)");
        var sb = new StringBuilder();
        sb.AppendLine("OPERA LEGACY LAB - REPORT");
        sb.AppendLine("Session: " + Lab.Code);
        sb.AppendLine("Started (UTC): " + Lab.CreatedUtc.ToString("u"));
        sb.AppendLine("Last seen (UTC): " + Lab.LastSeenUtc.ToString("u"));
        sb.AppendLine();
        sb.AppendLine("-- Server-verified --");
        sb.AppendLine("User-Agent: " + Lab.LastUserAgent);
        sb.AppendLine("HTTP protocol: " + Lab.LastProtocol);
        sb.AppendLine("Cookie round-trip: " + (Lab.CookieRoundTripPassed?.ToString() ?? "not tested"));
        sb.AppendLine("Cookie round-trip (HttpOnly+Secure+SameSite=Strict): " + (Lab.SecureCookieRoundTripPassed?.ToString() ?? "not tested"));
        sb.AppendLine("  - Secure actually enforced: " + (Lab.SecureCookieSeenOverPlainHttp is null ? "not tested" : (!Lab.SecureCookieSeenOverPlainHttp.Value).ToString()));
        sb.AppendLine("  - HttpOnly actually enforced: " + (Lab.HttpOnlyVisibleToJs is null ? "not tested" : (!Lab.HttpOnlyVisibleToJs.Value).ToString()));
        sb.AppendLine("  - SameSite=Strict actually enforced: not tested (needs a genuine cross-site request; this lab is single-origin)");
        sb.AppendLine("Form POST: " + (Lab.FormPosted ? "submitted" : "not submitted"));
        if (Lab.FormPosted)
            foreach (var kv in Lab.FormPostedValues)
                sb.AppendLine("  " + kv.Key + " = " + kv.Value);
        sb.AppendLine("WML result: " + (Lab.WmlResult ?? "not tested"));
        sb.AppendLine("WAP UAProf URL: " + (Lab.WapProfileUrl ?? "not advertised"));
        if (Lab.WapProfileUrl is not null)
            sb.AppendLine("WAP UAProf fetch status: " + (Lab.WapProfileFetchStatus ?? "not fetched yet"));
        sb.AppendLine();
        sb.AppendLine("-- Self-reported --");
        foreach (var t in ReportModel.SelfReportTests)
        {
            var result = Lab.SelfReports.TryGetValue(t.Key, out var r) ? r : "not tested";
            var note = Lab.SelfReportNotes.TryGetValue(t.Key, out var n) ? n : "";
            sb.AppendLine(t.Label + ": " + result + (string.IsNullOrEmpty(note) ? "" : "  (" + note + ")"));
        }
        sb.AppendLine();
        sb.AppendLine("-- All request headers (most recent request) --");
        foreach (var h in Lab.LastHeaders)
            sb.AppendLine(h.Name + ": " + h.Value);

        return Content(sb.ToString(), "text/plain; charset=utf-8", Encoding.UTF8);
    }
}
