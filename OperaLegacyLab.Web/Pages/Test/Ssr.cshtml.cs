using Microsoft.AspNetCore.Mvc;
using OperaLegacyLab.Web.Infrastructure;

namespace OperaLegacyLab.Web.Pages.Test;

public class SsrModel : LabPageModel
{
    private readonly DeviceResultLog _deviceLog;

    public SsrModel(LabSessionStore store, DeviceResultLog deviceLog) : base(store) => _deviceLog = deviceLog;

    public string LongToken { get; } = string.Concat(Enumerable.Repeat("abcdefghij", 12)); // 120 chars, no spaces

    public void OnGet() => ResolveLab("Small-screen rendering test");

    public IActionResult OnPost()
    {
        ResolveLab("Small-screen rendering test");
        var (result, note) = SelfReport.ReadForm(Request.Form);
        Lab.SelfReports["ssr"] = result;
        Lab.SelfReportNotes["ssr"] = note;
        _deviceLog.Record(HttpContext, "ssr", "Small-screen reflow", result, note);
        return Redirect(Markup.U("/report", Lab.Code));
    }
}
