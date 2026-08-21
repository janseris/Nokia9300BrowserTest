using Microsoft.AspNetCore.Mvc;
using OperaLegacyLab.Web.Infrastructure;

namespace OperaLegacyLab.Web.Pages.Test;

public class Css2Model : LabPageModel
{
    private readonly DeviceResultLog _deviceLog;

    public Css2Model(LabSessionStore store, DeviceResultLog deviceLog) : base(store) => _deviceLog = deviceLog;

    public void OnGet() => ResolveLab("Extended CSS capability test");

    public IActionResult OnPost()
    {
        ResolveLab("Extended CSS capability test");
        var (result, note) = SelfReport.ReadForm(Request.Form);
        Lab.SelfReports["css2"] = result;
        Lab.SelfReportNotes["css2"] = note;
        _deviceLog.Record(HttpContext, "css2", "Extended CSS capabilities", result, note);
        return Redirect(Markup.U("/report", Lab.Code));
    }
}
