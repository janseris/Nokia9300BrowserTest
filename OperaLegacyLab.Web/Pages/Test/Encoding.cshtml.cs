using Microsoft.AspNetCore.Mvc;
using OperaLegacyLab.Web.Infrastructure;

namespace OperaLegacyLab.Web.Pages.Test;

public class EncodingModel : LabPageModel
{
    private readonly DeviceResultLog _deviceLog;

    public EncodingModel(LabSessionStore store, DeviceResultLog deviceLog) : base(store) => _deviceLog = deviceLog;

    public void OnGet() => ResolveLab("Character encoding test");

    public IActionResult OnPost()
    {
        ResolveLab("Character encoding test");
        var (result, note) = SelfReport.ReadForm(Request.Form);
        Lab.SelfReports["encoding"] = result;
        Lab.SelfReportNotes["encoding"] = note;
        _deviceLog.Record(HttpContext, "encoding", "Character encoding", result, note);
        return Redirect(Markup.U("/report", Lab.Code));
    }
}
