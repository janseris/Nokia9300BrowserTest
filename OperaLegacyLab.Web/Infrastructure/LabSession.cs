namespace OperaLegacyLab.Web.Infrastructure;

/// <summary>
/// Everything the lab knows about one visiting browser. Identity is carried
/// either by a "lab" cookie (if the browser supports cookies) or by a "lab="
/// query-string parameter that every internal link carries as a fallback -
/// exactly the belt-and-braces approach a 2004-era WAP/web site would use,
/// since cookie support on phone browsers of that generation was unreliable.
/// </summary>
public sealed class LabSession
{
    public required string Code { get; init; }
    public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;

    // First and most recent request snapshots (server-observed, authoritative -
    // no self-report needed for any of this).
    public string? FirstUserAgent { get; set; }
    public string? LastUserAgent { get; set; }
    public string? LastProtocol { get; set; }
    public string? LastMethod { get; set; }
    public string? LastRemoteIp { get; set; }
    public DateTime LastSeenUtc { get; set; }
    public List<(string Name, string Value)> LastHeaders { get; set; } = new();

    // WAP UAProf (X-Wap-Profile) discovery.
    public string? WapProfileUrl { get; set; }
    public bool WapProfileFetchAttempted { get; set; }
    public string? WapProfileFetchStatus { get; set; }
    public string? WapProfileSnippet { get; set; }

    // Authoritative round-trip tests (server verifies directly, no self-report).
    public bool? CookieRoundTripPassed { get; set; }

    // Independent of CookieRoundTripPassed above: that test's cookie has no HttpOnly/Secure/SameSite
    // attributes at all (the simplest possible case). This one specifically checks whether a cookie
    // set with HttpOnly=true, Secure=true, SameSite=Strict - all attributes that postdate Opera 6.x
    // by many years (SameSite in particular is a 2016+ addition) - still round-trips, since a browser
    // that doesn't recognize an attribute could reasonably ignore it (harmless) or could choke on the
    // whole Set-Cookie header (not harmless) - either is a real, useful answer.
    public bool? SecureCookieRoundTripPassed { get; set; }

    // Two more direct, targeted follow-ups - SecureCookieRoundTripPassed above only proves the cookie
    // survived ONE https round trip, which is not the same as proving Secure/HttpOnly/SameSite are
    // actually being enforced. See CookieSecureCheck.cshtml.cs's own doc comment for exactly how each
    // is obtained and what it does/doesn't prove.
    //
    // Did the SAME cookie also come back on a later, deliberate plain-http (no TLS) visit to the same
    // check page? true = it did (Secure is NOT being enforced - a real gap); false = it didn't (Secure
    // IS being enforced correctly); null = that follow-up was never attempted (e.g. no plain-http path
    // to this host exists through whichever tunnel is in front of this app, or the visitor never
    // clicked the link).
    public bool? SecureCookieSeenOverPlainHttp { get; set; }

    // Did client-side JavaScript's own document.cookie see this cookie at all? true = yes (HttpOnly is
    // NOT being enforced); false = no, even though the cookie did arrive in the raw request header
    // (HttpOnly IS working); null = never determined (no JS ran at all, or the cookie never arrived in
    // the first place so there's nothing meaningful to conclude from document.cookie either way).
    public bool? HttpOnlyVisibleToJs { get; set; }

    public bool FormPosted { get; set; }
    public Dictionary<string, string> FormPostedValues { get; set; } = new();
    public string? WmlRequestAccept { get; set; }
    public string? WmlResult { get; set; } // "yes" / "no" / null

    // Self-reported observations (the visitor tells us what they saw on screen).
    public Dictionary<string, string> SelfReports { get; set; } = new();
    public Dictionary<string, string> SelfReportNotes { get; set; } = new();

    // Per-session TOTP secret for the /test/qr auto-refreshing QR code test.
    // Generated once on first visit to that test and held for the lifetime of
    // the (in-memory, non-persisted) lab session - see Totp.GetOrCreateSecret.
    public byte[]? QrTotpSecret { get; set; }

    public void RecordRequest(HttpContext ctx)
    {
        FirstUserAgent ??= ctx.Request.Headers.UserAgent.ToString();
        LastUserAgent = ctx.Request.Headers.UserAgent.ToString();
        LastProtocol = ctx.Request.Protocol;
        LastMethod = ctx.Request.Method;
        LastRemoteIp = ctx.Connection.RemoteIpAddress?.ToString();
        LastSeenUtc = DateTime.UtcNow;
        LastHeaders = ctx.Request.Headers
            .Select(h => (h.Key, string.Join(", ", h.Value.ToArray())))
            .OrderBy(h => h.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var wapProfile = ctx.Request.Headers["X-Wap-Profile"].ToString();
        if (!string.IsNullOrWhiteSpace(wapProfile) && string.IsNullOrEmpty(WapProfileUrl))
        {
            // Header value is sometimes wrapped in quotes: "http://..."
            WapProfileUrl = wapProfile.Trim().Trim('"');
        }
    }
}
