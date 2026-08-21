using Microsoft.AspNetCore.Mvc;
using OperaLegacyLab.Web.Infrastructure;

namespace OperaLegacyLab.Web.Pages.Test;

public class Js2Model : LabPageModel
{
    private readonly DeviceResultLog _deviceLog;

    public Js2Model(LabSessionStore store, DeviceResultLog deviceLog) : base(store) => _deviceLog = deviceLog;

    public void OnGet() => ResolveLab("Extended JavaScript capability test");

    public IActionResult OnPost()
    {
        ResolveLab("Extended JavaScript capability test");
        var (result, note) = SelfReport.ReadForm(Request.Form);
        Lab.SelfReports["js2"] = result;
        Lab.SelfReportNotes["js2"] = note;
        _deviceLog.Record(HttpContext, "js2", "Extended JavaScript capabilities", result, note);
        return Redirect(Markup.U("/report", Lab.Code));
    }
}
