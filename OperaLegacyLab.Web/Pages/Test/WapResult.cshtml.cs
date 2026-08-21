using OperaLegacyLab.Web.Infrastructure;

namespace OperaLegacyLab.Web.Pages.Test;

public class WapResultModel : LabPageModel
{
    private readonly DeviceResultLog _deviceLog;

    public WapResultModel(LabSessionStore store, DeviceResultLog deviceLog) : base(store) => _deviceLog = deviceLog;

    public void OnGet(string? ok)
    {
        ResolveLab("WML result");
        Lab.WmlResult = ok == "1" ? "yes" : "no";
        _deviceLog.Record(HttpContext, "wml", "WML rendering", Lab.WmlResult);
    }
}
