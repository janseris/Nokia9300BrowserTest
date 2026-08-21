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
