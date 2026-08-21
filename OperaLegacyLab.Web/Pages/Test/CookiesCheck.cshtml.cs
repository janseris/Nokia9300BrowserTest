using OperaLegacyLab.Web.Infrastructure;

namespace OperaLegacyLab.Web.Pages.Test;

public class CookiesCheckModel : LabPageModel
{
    private const string TestCookieName = "cookietest";

    private readonly DeviceResultLog _deviceLog;

    public CookiesCheckModel(LabSessionStore store, DeviceResultLog deviceLog) : base(store) => _deviceLog = deviceLog;

    public bool Present { get; private set; }
    public string RawCookieHeader { get; private set; } = "";

    public void OnGet()
    {
        ResolveLab("Cookie test result");
        Present = Request.Cookies.TryGetValue(TestCookieName, out var val) && val == "round-trip-ok";
        Lab.CookieRoundTripPassed = Present;
        RawCookieHeader = Request.Headers.Cookie.ToString();
        _deviceLog.Record(HttpContext, "cookies", "Cookie round-trip", Present ? "pass" : "fail");
    }
}
