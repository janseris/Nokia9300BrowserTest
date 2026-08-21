using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace OperaLegacyLab.Web.Infrastructure;

/// <summary>
/// In-memory store of lab sessions. This is a single-instance, single-process
/// diagnostic tool meant to be run for a short LAN test session, so a plain
/// ConcurrentDictionary is all that's needed - no database, no distributed cache.
/// </summary>
public sealed class LabSessionStore
{
    // Deliberately excludes visually-ambiguous characters (0/O, 1/I/L) since the
    // code may need to be read off a tiny phone screen and re-typed elsewhere.
    private const string Alphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";
    private readonly ConcurrentDictionary<string, LabSession> _sessions = new(StringComparer.OrdinalIgnoreCase);

    public const string CookieName = "lab";
    public const string QueryName = "lab";

    /// <summary>
    /// Resolves (or creates) the session for the current request, refreshes the
    /// "lab" cookie opportunistically, and records this request's diagnostics.
    /// Every endpoint should call this first.
    /// </summary>
    public LabSession Resolve(HttpContext ctx)
    {
        string? code = ctx.Request.Query[QueryName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(code))
            code = ctx.Request.Cookies[CookieName];

        LabSession? session = null;
        if (!string.IsNullOrWhiteSpace(code))
            _sessions.TryGetValue(code, out session);

        if (session is null)
        {
            code = GenerateCode();
            session = new LabSession { Code = code };
            _sessions[code] = session;
        }

        // Set the cookie on every response. If the browser honors cookies this
        // becomes a no-op after the first request; if it doesn't, it costs nothing
        // and the "lab=" query parameter carried by every link keeps identity.
        ctx.Response.Cookies.Append(CookieName, session.Code, new CookieOptions
        {
            Path = "/",
            IsEssential = true,
        });

        session.RecordRequest(ctx);
        return session;
    }

    public bool TryGet(string code, out LabSession session)
    {
        var found = _sessions.TryGetValue(code, out var s);
        session = s!;
        return found;
    }

    private string GenerateCode()
    {
        Span<byte> buf = stackalloc byte[6];
        string code;
        do
        {
            RandomNumberGenerator.Fill(buf);
            var chars = new char[6];
            for (int i = 0; i < 6; i++)
                chars[i] = Alphabet[buf[i] % Alphabet.Length];
            code = new string(chars);
        } while (_sessions.ContainsKey(code));
        return code;
    }
}
