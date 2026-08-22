# Opera Legacy Lab

A small ASP.NET Core 10 web app for finding out exactly what an old mobile
browser can do. Built with the Nokia 9300 (Symbian S80) and its built-in
Opera 6.x browser in mind, but it works against any browser.

Starting point we know for Opera 6.x on the 9300: HTML 4.01, XHTML,
JavaScript 1.3, WML 1.3, TLS 1.2 (via a device-side patch), and Opera's
"Small Screen Rendering" reflow. This app tries to discover a lot more than
that, using two kinds of tests:

- **Server-verified tests** - the server checks the result directly, no
  self-reporting needed: raw request/header dump, a cookie set/round-trip
  check, a form POST that echoes back exactly what was received, a WAP
  UAProf (`X-Wap-Profile`) lookup if the browser sends one, and a WML card
  with reply links.
- **Self-reported tests** - HTML rendering, CSS, JavaScript (including
  `alert`/`confirm`/`prompt`), character encoding, small-screen reflow and
  frames. Each page renders something and asks a plain HTML form ("did this
  look right?") what you actually saw, since the server can't see the screen.

Everything a session has learned about a browser accumulates on `/report`
(HTML) and `/report/text` (plain text, easy to copy/paste or save).

Every result recorded from a request whose User-Agent contains `Nokia9300`
(the real device, not a desktop browser used while building this) is also
written straight to a plain-text file, `OperaLegacyLab.Web/device-results.txt`,
as it happens - so you can watch the actual phone's results land in a text
file without needing a browser tab open on `/report`. Re-running a test on
the device overwrites its one line there rather than adding a duplicate,
since the point is "what does the device show right now for this test," not
a full history of every attempt. This file (and a small machine-readable
`device-results.state.json` beside it) is gitignored, but both now **survive
an app restart** - only re-running the same test on the device replaces its
line, restarting the app itself no longer wipes anything. (An earlier
version of this deleted the file on every startup, which quietly threw away
real device results across the frequent restarts a normal dev loop involves
- fixed once that turned out to matter.)

## Why it's built this way

Every route is a Razor Page (`Pages/**/*.cshtml` + a code-behind
`.cshtml.cs`) - that's just an authoring convenience (no more hand-building
HTML inside C# strings), not a change to what's sent over the wire: no
Bootstrap, no bundling, no client asset pipeline, no JS/CSS framework of any
kind. The markup in each `.cshtml` file is still deliberately hand-written
HTML 4.01/XHTML/WML, because the whole point is controlling exactly which
bytes an early-2000s browser has to parse. The shared page chrome (in
`Pages/Shared/_Layout.cshtml`) uses no CSS at all, since CSS support is one
of the things being tested, not assumed. A couple of pages that need
byte-exact, non-UTF-8 output (the ISO-8859-1 encoding test, the WML card,
the plain-text report) bypass Razor's view rendering entirely and return a
raw `Content(...)` result instead, since Razor's view engine always writes
UTF-8 regardless of the declared charset.

The app tracks each visiting browser with a short 6-character "lab" code.
It's carried both as a cookie **and** as a `?lab=CODE` query parameter on
every link, because WAP-era phone browsers had unreliable cookie support -
if cookies don't work, navigation and the report still work via the URL.

The phone can't reach this app over the LAN, so the only access path is a
public tunnel (ngrok, Cloudflare Tunnel/cloudflared, or similar) pointed at
one of this app's two local listeners. The tunnel terminates the real TLS
connection with the phone at its own public edge, then makes a *separate*
local connection to this app to forward the request - which local listener
that connection targets depends on the tunnel:
- **Plain http (default `5000`)**: the simplest setup, and both ngrok's and
  cloudflared's own quickstarts default to exactly this - the tunnel
  decrypts its own public HTTPS at its edge and forwards plain HTTP
  locally. No certificate is involved on this hop at all, and it's equally
  secure from the phone's point of view either way.
- **HTTPS (default `5253`)**: only needed if you specifically want the
  tunnel-to-Kestrel hop itself encrypted/verified too. This listener serves
  the standard ASP.NET Core HTTPS **development** certificate (the one
  `dotnet dev-certs https` manages), not a one-off self-signed certificate -
  ngrok doesn't verify the origin certificate by default so it connects to
  either listener happily, but cloudflared *does* verify by default, so it
  will only accept this listener once you've run `dotnet dev-certs https
  --trust` once, locally (this adds the certificate to your OS's normal
  trust store).

The app also trusts the tunnel's `X-Forwarded-Proto`/`X-Forwarded-For`
headers (via `ForwardedHeadersOptions`) so `/diagnostics` reports the
phone's real IP address rather than the tunnel daemon's local one. Both
listeners are HTTP/1.1 only, and Kestrel's minimum-data-rate timeouts are
relaxed, since a phone on a slow mobile link can be much slower than
Kestrel's defaults expect.

Alert/confirm/prompt dialogs are tested too (at your explicit request), even
though a hung dialog can leave an old mobile browser unresponsive with no
clean way to dismiss it - you've said you can restart the device if needed.
To limit the damage if that happens: they live in their own section, after
the main [1]-[14] self-report form, so anything recorded before you trigger
a dialog survives even if the phone needs a hard restart afterwards. Each is
triggered by its own button and records its result automatically via
`location.href` right after you respond to it - no separate submit step,
since there may be no working page left to submit from.

## Running it

Requires the .NET 10 SDK. From Visual Studio: open `OperaLegacyLab.sln` and
run the `OperaLegacyLab.Web` project (F5 or Ctrl+F5). From the command line:

```bash
cd OperaLegacyLab.Web
dotnet run
```

By default it listens on `http://0.0.0.0:5000` **and** `https://0.0.0.0:5253`
(change with `dotnet run --LabPort=8080 --LabHttpsPort=8443`). The HTTPS
listener uses the standard ASP.NET Core HTTPS development certificate - if
you've never run it before, `dotnet dev-certs https` (no arguments) creates
one; add `--trust` to also add it to your OS's trust store, which matters
only if your tunnel client verifies the origin certificate (see below).

## Testing through a tunnel (ngrok / Cloudflare Tunnel / similar)

1. Start the app (`dotnet run`, or F5 in Visual Studio).
2. In another terminal, start your tunnel pointed at whichever local listener
   it can reach:
   - ngrok, https origin: `ngrok http https://localhost:5253` - connects
     straight away, no extra flag needed, since ngrok doesn't verify the
     origin certificate by default.
   - cloudflared (Cloudflare Tunnel), http origin - the simplest option,
     and the one to reach for first if you hit a certificate-trust error:
     `cloudflared tunnel --url http://localhost:5000`.
   - cloudflared, https origin - only if you want this hop encrypted too:
     run `dotnet dev-certs https --trust` once first, then
     `cloudflared tunnel --url https://localhost:5253`. Without the
     `--trust` step first, cloudflared will reject the certificate as
     untrusted - that's the same failure a self-signed certificate would
     produce, and is exactly what the `--trust` step fixes.
3. Open the public URL the tunnel gives you in an ordinary desktop browser
   first, to confirm the tunnel itself works before involving the phone.
4. A couple of things worth knowing about this path specifically:
   - **Some tunnel providers show an interstitial warning page** (ngrok's
     free tier does: "you are about to visit...") before proxying through to
     the real site. That page is modern HTML/CSS/JS - there's a real chance
     the 9300's Opera can't render or click through it at all, which would
     block access regardless of anything this app does. If the phone can't
     get past that page, that's the tunnel provider's warning page, not a
     capability this app is testing.
   - Once the tunnel is up, the URL is effectively public for as long as it
     stays open - anyone with the link can reach it. There's nothing
     sensitive behind it (just self-reported browser test results), but
     it's worth closing the tunnel when you're done.
5. On the phone, go to the tunnel's public HTTPS URL. Work through the
   numbered tests from the home page. If a "lab session code" keeps changing
   every time you go back to the home page, that's itself useful information
   - it means cookies aren't working.
6. Check `/report` (or `/report/text`) at any point to see everything
   gathered so far. Since the phone can't easily save a file, note down the
   6-character session code shown at the bottom of every page - you can open
   the same report later from a desktop browser at the same tunnel URL plus
   `/report?lab=<CODE>` (while the tunnel is still up).

## Project layout

Every route is a Razor Page - there is no `app.MapGet`/`app.MapPost` call
anywhere in `Program.cs`, just a single `app.MapRazorPages()`. Each page below
is a `.cshtml` + `.cshtml.cs` pair unless noted otherwise.

```
OperaLegacyLab.sln
OperaLegacyLab.Web/
  Program.cs                      Kestrel/host setup (HTTP + HTTPS), forwarded-headers, app.MapRazorPages()
  Infrastructure/
    LabSession.cs                 Per-browser state
    LabSessionStore.cs            In-memory session store + cookie/URL resolution
    LabPageModel.cs                Shared PageModel base: resolves the lab session, carries [IgnoreAntiforgeryToken]
    Markup.cs                      Small helpers: HTML-escaping, "?lab=" URL builder
    SelfReport.cs                  Shared "did this look right?" self-post form
    DevCertificate.cs              RETIRED, unused - safe to delete (see its own doc comment)
  Pages/
    _ViewStart.cshtml              Wires every page to Shared/_Layout.cshtml
    Shared/_Layout.cshtml          Page chrome: HTML 4.01 doctype, nav links, no CSS
    Index.cshtml                   /                              home page, links to every test
    Diagnostics.cshtml             /diagnostics                   raw request/header dump
    UaProf.cshtml                  /uaprof                        WAP UAProf (X-Wap-Profile) fetch-and-show
    Report.cshtml                  /report                        aggregated server-verified + self-reported results
    ReportText.cshtml              /report/text                   same, as plain text (Content() bypass)
    Test/
      Html.cshtml                  /test/html                     HTML 4.01 rendering
      Css.cshtml                   /test/css                      CSS support
      Js.cshtml                    /test/js                       JS [1]-[14] + alert/confirm/prompt buttons
      JsDialogResult.cshtml        /test/js/dialogresult          records alert/confirm/prompt outcome
      Forms.cshtml                 /test/forms                    form POST field echo
      Cookies.cshtml               /test/cookies                  sets the round-trip test cookie, redirects
      CookiesCheck.cshtml          /test/cookies/check             confirms the cookie came back
      Encoding.cshtml              /test/encoding                  links to the two variants below
      EncodingUtf8.cshtml          /test/encoding/utf8              raw UTF-8 bytes (Html.Raw, not auto-encoded)
      EncodingLatin1.cshtml        /test/encoding/latin1            raw ISO-8859-1 bytes (Content() bypass)
      Ssr.cshtml                   /test/ssr                        wide layout / Small Screen Rendering reflow
      Frames.cshtml                /test/frames                     info + self-report
      FramesView.cshtml            /test/frames/view                 the <frameset> document (Layout = null)
      FramesNav.cshtml              /test/frames/nav                  nav frame (Layout = null)
      FramesContent.cshtml          /test/frames/content               content frame (Layout = null)
      FramesContent2.cshtml         /test/frames/content2               content frame after nav click (Layout = null)
      Wap.cshtml                    /test/wap                        info page, Accept header check
      WapWml.cshtml                 /test/wap.wml                     the actual WML 1.3 card (Content() bypass)
      WapResult.cshtml              /test/wap/result                  records the WML yes/no reply
  wwwroot/img/                     test.gif / test.png / test.jpg
```

Two things worth knowing if you're extending this:

- **Antiforgery is disabled app-wide**, via `[IgnoreAntiforgeryToken]` on the
  shared `LabPageModel` base class (inherited by every page). Razor Pages
  auto-validates a token on every POST by default, but every self-report form
  here is a deliberately bare `<form method="post">` with no hidden token
  field (see `SelfReport.cs`) - and the token check's double-submit-cookie
  requirement would fail unpredictably on exactly the cookie-unreliable
  browsers this lab exists to test. There's no authenticated state here worth
  protecting from CSRF.
- **Razor's default output encoding numeric-entity-escapes non-ASCII
  characters** (e.g. writes `&#233;` instead of a literal byte sequence for
  e-acute). That's invisible on most pages, but it would have silently broken
  the entire point of the UTF-8 and ISO-8859-1 encoding tests - both send
  their sample text unescaped (`Html.Raw(...)` on the UTF-8 page, a plain
  string with no HTML metacharacters on the ISO-8859-1 page) so what actually
  reaches the browser is the real multi-byte/single-byte encoding, not an
  ASCII-safe stand-in for it.

## Notes and known limitations

- TLS specifics of the *phone's* connection (protocol version, cipher suite)
  aren't visible from inside this app - whichever tunnel is in front (ngrok,
  cloudflared, or similar) terminates that connection at its own public
  edge, then makes a separate local connection to this app to forward the
  request. `/diagnostics` shows that the request arrived as https, which is
  the confirmation available from this side.
- The WAP UAProf fetch reaches out to whatever URL the phone's
  `X-Wap-Profile` header advertises. That's usually hosted by the handset or
  browser vendor - it may well be offline after 20+ years, in which case a
  fetch failure is itself the answer, not a bug in this app.
- The JavaScript test page puts each feature in its own `<script>` block on
  purpose, so a parse or runtime error in one (e.g. `try/catch`, which
  predates JS1.3) doesn't necessarily hide whether the ones before and after
  it worked.
- This is a single-process, in-memory tool - sessions are lost when the
  process restarts. Fine for a test session; don't expect history to survive
  a redeploy or a Visual Studio rebuild-and-relaunch.
