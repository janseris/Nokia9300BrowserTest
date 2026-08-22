using Microsoft.AspNetCore.Mvc;
using OperaLegacyLab.Web.Infrastructure;

namespace OperaLegacyLab.Web.Pages.Test;

/// <summary>
/// A cookie carrying HttpOnly/Secure/SameSite=Strict either surviving one https round trip (that's all
/// the OTHER fields on this page ever proved) is NOT the same as any of those three attributes actually
/// being enforced by the browser - a browser that ignores all three completely and one that enforces
/// all three correctly look identical under that one test. This page adds two real, targeted follow-ups
/// on top of the basic round trip:
///
/// SECURE - only actually means something if the SAME cookie is also tried over a plain http connection
/// to the very same host. If it still comes back there, Secure is not being enforced; if it's genuinely
/// absent there (while the ordinary "lab" cookie, which has no Secure flag, still shows up fine), that's
/// real evidence Secure IS enforced. That requires a second, deliberate visit over http - offered below
/// as a plain link the visitor has to click - which in turn only means anything if a plain-http path to
/// this exact host actually exists (on the real device this app is reached exclusively through ngrok's
/// https tunnel - see Program.cs - so this may simply be unreachable there; that's a real, documented
/// limitation of the test environment, not a bug in the test).
///
/// HTTPONLY - HttpOnly's entire point is hiding a cookie from JavaScript's document.cookie, so the only
/// way to check it is to actually ask JavaScript. The GET view below writes out a tiny self-submitting
/// form (document.write + immediate .submit(), no user click needed - the same document.write-driven
/// technique already used throughout Js.cshtml/Js2.cshtml, so if JS runs there it'll run here) carrying
/// whether document.cookie contained this test cookie's name. OnPost below records that, then redirects
/// back to this same GET (POST/redirect/GET) so the result renders as a normal page instead of looping -
/// critically, that redirect target sets probed=1 so the GET view knows NOT to write out another
/// self-submitting probe form on top of an answer it already has.
///
/// SAMESITE=STRICT - deliberately NOT tested here. SameSite only ever matters for a request that
/// originates from a genuinely different site than the cookie's own origin; everything reachable from
/// this lab is same-origin navigation, so there is no request this app can make (or ask a visitor to
/// make) that would ever give SameSite a chance to block anything. Testing it for real would need actual
/// cross-site infrastructure (e.g. a link hosted on a second, different domain) that this single-origin
/// lab doesn't have - so this is reported as an explicit, honest gap rather than a fabricated result.
/// </summary>
public class CookieSecureCheckModel : LabPageModel
{
    private const string TestCookieName = "cookietest_secure";

    private readonly DeviceResultLog _deviceLog;

    public CookieSecureCheckModel(LabSessionStore store, DeviceResultLog deviceLog) : base(store) => _deviceLog = deviceLog;

    public bool Present { get; private set; }
    public bool RequestWasHttps { get; private set; }
    public string RawCookieHeader { get; private set; } = "";

    /// <summary>True once the JS self-submit probe has already produced an answer (or this exact GET
    /// arrived via the post-probe redirect) - tells the view not to write out another probe form.</summary>
    public bool HttpOnlyAlreadyProbed { get; private set; }

    /// <summary>Absolute http:// URL for the SAME host/path, for the "try over plain http" link. Built
    /// from the request's own (possibly ngrok-forwarded) host, no port - correct for the real ngrok
    /// deployment where both schemes share one public hostname on standard ports; not meaningful for
    /// this sandbox's own dev setup, where http/https are two different ports on localhost.</summary>
    public string PlainHttpCheckUrl { get; private set; } = "";

    public void OnGet(int probed = 0)
    {
        ResolveLab("Secure/HttpOnly/SameSite cookie test - result");
        Present = Request.Cookies.TryGetValue(TestCookieName, out var val) && val == "round-trip-ok";
        RequestWasHttps = Request.IsHttps;
        RawCookieHeader = Request.Headers.Cookie.ToString();

        if (RequestWasHttps)
        {
            // The original round-trip result - only overwritten by an https visit, so a LATER plain-http
            // follow-up (see below) can never clobber what the https visit already established.
            Lab.SecureCookieRoundTripPassed = Present;
        }
        else
        {
            // This is the deliberate plain-http follow-up: same cookie, same host, no TLS. Whether it's
            // present here is the real Secure-enforcement signal - see this class's own doc comment.
            Lab.SecureCookieSeenOverPlainHttp = Present;
        }

        HttpOnlyAlreadyProbed = probed == 1 || Lab.HttpOnlyVisibleToJs.HasValue;
        PlainHttpCheckUrl = $"http://{Request.Host.Host}/test/cookie-secure/check?lab={Lab.Code}";

        RecordDeviceLog();
    }

    /// <summary>Receives the JS self-submit probe's finding (see this class's own doc comment), then
    /// redirects back to OnGet with probed=1 so the resulting page renders as a normal result instead of
    /// writing out (and immediately re-submitting) another copy of the same probe form.</summary>
    public IActionResult OnPost(string httpOnlyVisible)
    {
        ResolveLab("Secure/HttpOnly/SameSite cookie test - result");
        Lab.HttpOnlyVisibleToJs = httpOnlyVisible == "1";
        RecordDeviceLog();
        return Redirect($"/test/cookie-secure/check?lab={Lab.Code}&probed=1");
    }

    private void RecordDeviceLog()
    {
        var parts = new List<string>
        {
            "https round-trip=" + (Lab.SecureCookieRoundTripPassed?.ToString() ?? "not tested"),
        };
        if (Lab.SecureCookieSeenOverPlainHttp.HasValue)
            parts.Add("Secure enforced=" + (!Lab.SecureCookieSeenOverPlainHttp.Value));
        if (Lab.HttpOnlyVisibleToJs.HasValue)
            parts.Add("HttpOnly enforced=" + (!Lab.HttpOnlyVisibleToJs.Value));

        _deviceLog.Record(HttpContext, "cookies_secure", "Secure/HttpOnly/SameSite cookie test",
            Lab.SecureCookieRoundTripPassed == true ? "pass" : "fail", string.Join(", ", parts));
    }
}
