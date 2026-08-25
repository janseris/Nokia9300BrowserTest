using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OperaLegacyLab.Web.Pages;

/// <summary>
/// The absolute minimum this app can serve: two bytes of plain text, no session cookie, no _Layout,
/// no nav chrome, no LabSessionStore dependency at all - nothing PageSizeReportMiddleware needs to
/// touch either (no PageSizeBanner.Marker means it passes straight through untouched).
///
/// Lives at /OK (not "/") specifically so the real home page keeps its usual place - this exists
/// purely to test raw reachability through a new tunnel/device combination (e.g. a Nokia 5130
/// XpressMusic tethered via PC Suite over EDGE) before blaming anything this app renders. If the
/// phone's browser still hangs at "Loading..." on this page, the fault is below the app layer - the
/// tunnel, TLS, or the network path - not the Razor Pages pipeline, the lab session cookie, or any of
/// the markup the home page sends. If this DOES load, point the phone at / next for the actual test
/// hub.
/// </summary>
public class OKModel : PageModel
{
    public IActionResult OnGet()
    {
        // no-store: repeated hits while diagnosing a flaky tunnel should never be answered from a
        // cache (the tunnel's, a proxy's, or the phone browser's own) instead of a fresh request.
        Response.Headers.CacheControl = "no-store";
        return Content("OK", "text/plain");
    }
}
