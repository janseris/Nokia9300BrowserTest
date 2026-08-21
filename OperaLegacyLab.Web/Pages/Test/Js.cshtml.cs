using Microsoft.AspNetCore.Mvc;
using OperaLegacyLab.Web.Infrastructure;

namespace OperaLegacyLab.Web.Pages.Test;

public class JsModel : LabPageModel
{
    private readonly DeviceResultLog _deviceLog;

    public JsModel(LabSessionStore store, DeviceResultLog deviceLog) : base(store) => _deviceLog = deviceLog;

    public void OnGet() => ResolveLab("JavaScript capability test");

    public IActionResult OnPost()
    {
        ResolveLab("JavaScript capability test");
        var (result, note) = SelfReport.ReadForm(Request.Form);
        Lab.SelfReports["js"] = result;
        Lab.SelfReportNotes["js"] = note;
        _deviceLog.Record(HttpContext, "js", "JavaScript [1]-[14]", result, note);
        return Redirect(Markup.U("/report", Lab.Code));
    }
}
