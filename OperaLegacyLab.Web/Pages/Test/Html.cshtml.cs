using Microsoft.AspNetCore.Mvc;
using OperaLegacyLab.Web.Infrastructure;

namespace OperaLegacyLab.Web.Pages.Test;

public class HtmlModel : LabPageModel
{
    private readonly DeviceResultLog _deviceLog;

    public HtmlModel(LabSessionStore store, DeviceResultLog deviceLog) : base(store) => _deviceLog = deviceLog;

    public void OnGet() => ResolveLab("HTML rendering test");

    public IActionResult OnPost()
    {
        ResolveLab("HTML rendering test");
        var (result, note) = SelfReport.ReadForm(Request.Form);
        Lab.SelfReports["html"] = result;
        Lab.SelfReportNotes["html"] = note;
        _deviceLog.Record(HttpContext, "html", "HTML rendering", result, note);
        return Redirect(Markup.U("/report", Lab.Code));
    }
}
