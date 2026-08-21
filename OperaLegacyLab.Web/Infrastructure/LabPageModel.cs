using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OperaLegacyLab.Web.Infrastructure;

/// <summary>
/// Shared base for every Razor Page that needs the current lab session.
/// Centralizes session resolution (reads/creates the "lab" cookie, refreshes
/// it on the response) and hands the session code to the shared _Layout via
/// ViewData, so individual pages don't each repeat that wiring.
///
/// [IgnoreAntiforgeryToken] here applies to every derived page (attributes on
/// a base class are inherited by default). Razor Pages auto-validates an
/// antiforgery token on every POST unless told otherwise, but every
/// self-report/self-post form in this app is a deliberately bare
/// &lt;form method="post"&gt; with no hidden token field (see SelfReport.cs)
/// - the whole point is testing plain HTML 4.01 form support on a
/// 20-year-old browser, and the antiforgery check's double-submit-cookie
/// requirement would fail unpredictably on exactly the cookie-unreliable
/// browsers this lab exists to test. This is a throwaway diagnostic tool
/// with no authenticated state worth protecting from CSRF.
/// </summary>
[IgnoreAntiforgeryToken]
public abstract class LabPageModel : PageModel
{
    protected readonly LabSessionStore Store;

    protected LabPageModel(LabSessionStore store) => Store = store;

    public LabSession Lab { get; private set; } = null!;

    /// <summary>Call first thing in OnGet/OnPost. Also sets ViewData["Title"].</summary>
    protected void ResolveLab(string title)
    {
        Lab = Store.Resolve(HttpContext);
        ViewData["Title"] = title;
        ViewData["LabCode"] = Lab.Code;
    }
}
