using Microsoft.AspNetCore.Mvc;
using OperaLegacyLab.Web.Infrastructure;

namespace OperaLegacyLab.Web.Pages.Test;

/// <summary>
/// Whether this browser supports HTTP compression, split into the two things that question actually
/// conflates: whether it ASKS for a compressed response (the Accept-Encoding request header - the
/// only "compression" a browser itself ever sends; browsers of this era have no way to compress a
/// request body, that's not a thing outside of manual JS-side encoding on a much later browser) is
/// server-verified directly from this page's own request, no self-report needed. Whether it can
/// actually DECOMPRESS one needs the round-trip resource pages (CompressionGzip/Deflate/DeflateRaw) -
/// which deliberately send Content-Encoding regardless of what Accept-Encoding said, specifically to
/// discover what the browser can really handle rather than only what it claims to.
/// </summary>
public class CompressionModel : LabPageModel
{
    private readonly DeviceResultLog _deviceLog;

    public CompressionModel(LabSessionStore store, DeviceResultLog deviceLog) : base(store) => _deviceLog = deviceLog;

    public string AcceptEncoding { get; private set; } = "";
    public string Te { get; private set; } = "";

    public void OnGet()
    {
        ResolveLab("HTTP compression test");
        AcceptEncoding = Request.Headers.AcceptEncoding.ToString();
        Te = Request.Headers["TE"].ToString();
    }

    public IActionResult OnPost()
    {
        ResolveLab("HTTP compression test");
        var (result, note) = SelfReport.ReadForm(Request.Form);
        Lab.SelfReports["compression"] = result;
        Lab.SelfReportNotes["compression"] = note;
        _deviceLog.Record(HttpContext, "compression", "HTTP compression (gzip/deflate)", result, note);
        return Redirect(Markup.U("/report", Lab.Code));
    }
}
