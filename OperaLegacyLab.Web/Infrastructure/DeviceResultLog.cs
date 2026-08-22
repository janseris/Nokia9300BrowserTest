using System.Text.Json;

namespace OperaLegacyLab.Web.Infrastructure;

/// <summary>
/// Writes a plain-text summary of test results to a file on disk
/// (<c>device-results.txt</c> next to the project), but only for requests
/// whose User-Agent contains "Nokia9300" - i.e. the real phone, not a
/// desktop browser used to poke around the app while building it. This
/// lets you read the actual device's results directly from a text file
/// instead of having to load /report in a browser (which the phone itself
/// is busy using).
///
/// Results are keyed by test name, not by lab session: re-running the same
/// test on the device overwrites its one line rather than appending a
/// duplicate, since the point is "what did the device just show for this
/// test", not a full history of every attempt.
///
/// PERSISTENCE: results now survive an app restart. They didn't originally -
/// the constructor used to delete device-results.txt on every startup, on
/// the reasoning that in-memory session state doesn't survive a restart
/// either, so an old file could otherwise be mistaken for this run's
/// results. That made sense in isolation but fought the actual goal (a
/// durable written record to build a feature-support summary from) - this
/// app gets rebuilt/restarted constantly during development, so real
/// device results recorded in one run were quietly wiped the next time the
/// app started, with no warning. Fixed by keeping a small machine-readable
/// sidecar, <c>device-results.state.json</c> (also gitignored), that's
/// loaded back into memory on startup instead of deleted - device-results.txt
/// itself stays exactly the same human-readable format as before, just no
/// longer reset to blank on every run. A test is only ever replaced by
/// re-running that SAME test on the device again, never by merely
/// restarting the app.
/// </summary>
public sealed class DeviceResultLog
{
    private const string DeviceUserAgentMarker = "Nokia9300";

    // Fixes a stable, readable order for the file regardless of the order
    // tests happen to run in; anything unrecognized still gets appended
    // after these, rather than silently dropped.
    private static readonly string[] TestOrder =
    {
        "html", "css", "css2", "js",
        "js_dialog_alert", "js_dialog_confirm", "js_dialog_prompt", "js2",
        "encoding", "compression", "ssr", "frames", "qr_image", "qr_table",
        "cookies", "forms", "wml", "uaprof",
        "cookies_secure",
    };

    private readonly string _filePath;
    private readonly string _statePath;
    private readonly object _lock = new();
    private readonly Dictionary<string, string> _lines = new(StringComparer.OrdinalIgnoreCase);

    public DeviceResultLog(IWebHostEnvironment env)
    {
        _filePath = Path.Combine(env.ContentRootPath, "device-results.txt");
        _statePath = Path.Combine(env.ContentRootPath, "device-results.state.json");

        // Reload whatever the device already told us in a PREVIOUS run of this app - see the
        // PERSISTENCE note above. Best-effort: a missing/corrupt state file just means starting
        // from empty, same as before this fix existed.
        try
        {
            if (File.Exists(_statePath))
            {
                var saved = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(_statePath));
                if (saved is not null)
                {
                    foreach (var (key, line) in saved)
                        _lines[key] = line;
                }
            }
        }
        catch (IOException)
        {
        }
        catch (JsonException)
        {
        }

        if (_lines.Count > 0)
            WriteFile();
    }

    public static bool IsFromDevice(HttpContext ctx) =>
        ctx.Request.Headers.UserAgent.ToString()
            .Contains(DeviceUserAgentMarker, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Records (or overwrites) one test's result. No-ops entirely if the
    /// request isn't from the device, so nothing from desktop-browser
    /// testing during development ever lands in this file.
    /// </summary>
    public void Record(HttpContext ctx, string testKey, string label, string result, string? note = null)
    {
        if (!IsFromDevice(ctx)) return;

        var line = string.IsNullOrWhiteSpace(note)
            ? $"{label}: {result}"
            : $"{label}: {result}  ({note})";

        lock (_lock)
        {
            _lines[testKey] = line;
            WriteFile();
        }
    }

    private void WriteFile()
    {
        var ordered = TestOrder
            .Where(_lines.ContainsKey)
            .Concat(_lines.Keys.Where(k => !TestOrder.Contains(k, StringComparer.OrdinalIgnoreCase)))
            .Select(k => _lines[k]);

        var text = "OPERA LEGACY LAB - DEVICE TEST RESULTS" + Environment.NewLine +
                   "(requests whose User-Agent contains \"Nokia9300\" only - re-running a test replaces its line;" + Environment.NewLine +
                   " results now survive an app restart too - see device-results.state.json)" + Environment.NewLine +
                   "Last updated (UTC): " + DateTime.UtcNow.ToString("u") + Environment.NewLine +
                   Environment.NewLine +
                   string.Join(Environment.NewLine, ordered) + Environment.NewLine;

        // Best-effort: if this fails (e.g. file locked by an editor with a
        // strict share mode), the test result itself is still recorded in
        // the in-memory LabSession as normal - this file is a convenience,
        // not the source of truth.
        try
        {
            File.WriteAllText(_filePath, text);
            File.WriteAllText(_statePath, JsonSerializer.Serialize(_lines));
        }
        catch (IOException)
        {
        }
    }
}
