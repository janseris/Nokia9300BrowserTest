using Microsoft.AspNetCore.Mvc;
using OperaLegacyLab.Web.Infrastructure;

namespace OperaLegacyLab.Web.Pages.Test;

public class CookiesModel : LabPageModel
{
    private const string TestCookieName = "cookietest";

    public CookiesModel(LabSessionStore store) : base(store) { }

    // Sets a dedicated test cookie (independent of the "lab" session cookie)
    // and redirects. This isolates the cookie test itself from the
    // session-tracking mechanism, so it stays authoritative either way.
    public IActionResult OnGet()
    {
        ResolveLab("Cookie support test");
        Response.Cookies.Append(TestCookieName, "round-trip-ok", new CookieOptions
        {
            Path = "/",
            IsEssential = true,
        });
        return Redirect(Markup.U("/test/cookies/check", Lab.Code));
    }
}
