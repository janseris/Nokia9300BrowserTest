using Microsoft.AspNetCore.Mvc;
using OperaLegacyLab.Web.Infrastructure;

namespace OperaLegacyLab.Web.Pages.Test;

public class CssModel : LabPageModel
{
    private readonly DeviceResultLog _deviceLog;

    public CssModel(LabSessionStore store, DeviceResultLog deviceLog) : base(store) => _deviceLog = deviceLog;

    public void OnGet() => ResolveLab("CSS support test");

    public IActionResult OnPost()
    {
        ResolveLab("CSS support test");
        var (result, note) = SelfReport.ReadForm(Request.Form);
        Lab.SelfReports["css"] = result;
        Lab.SelfReportNotes["css"] = note;
        _deviceLog.Record(HttpContext, "css", "CSS support", result, note);
        return Redirect(Markup.U("/report", Lab.Code));
    }
}
