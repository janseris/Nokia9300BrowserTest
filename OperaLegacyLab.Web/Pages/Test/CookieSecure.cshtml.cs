using Microsoft.AspNetCore.Mvc;
using OperaLegacyLab.Web.Infrastructure;

namespace OperaLegacyLab.Web.Pages.Test;

public class CookieSecureModel : LabPageModel
{
    private const string TestCookieName = "cookietest_secure";

    public CookieSecureModel(LabSessionStore store) : base(store) { }

    // Sets a dedicated test cookie - independent of both the "lab" session
    // cookie and the plain "cookietest" cookie from /test/cookies - with
    // HttpOnly=true, Secure=true, SameSite=Strict, then redirects. A browser
    // over plain http (no ngrok/TLS in front of it) will never see this
    // cookie come back at all, since Secure requires an https connection;
    // that's expected, not a bug in the test - the real device is reached
    // exclusively over ngrok's https tunnel (see Program.cs), so this test
    // is only meaningful run that way.
    public IActionResult OnGet()
    {
        ResolveLab("Secure/HttpOnly/SameSite cookie test");
        Response.Cookies.Append(TestCookieName, "round-trip-ok", new CookieOptions
        {
            Path = "/",
            IsEssential = true,
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
        });
        return Redirect(Markup.U("/test/cookie-secure/check", Lab.Code));
    }
}
