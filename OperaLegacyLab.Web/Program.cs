using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using OperaLegacyLab.Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Nokia 9300 / Opera 6.x era browsers speak plain HTTP/1.1 - no HTTP/2. TLS 1.2
// works on the phone (confirmed via a device-side patch), reached exclusively
// through an ngrok tunnel (the phone can't reach this app over the LAN). ngrok
// terminates the real TLS connection with the phone at its own public edge -
// this app's own HTTPS listener on LabHttpsPort exists only so ngrok's agent
// has a local HTTPS backend to connect to (`ngrok http https://localhost:5443`);
// ngrok does not verify that certificate by default, so the self-signed one
// generated below just works with no extra configuration on either side.
// Bind explicitly to all network interfaces (not just localhost) so ngrok (or
// anything else on this machine) can reach either listener.
var port = builder.Configuration.GetValue<int?>("LabPort") ?? 5000;
var httpsPort = builder.Configuration.GetValue<int?>("LabHttpsPort") ?? 5253;
var certPath = Path.Combine(builder.Environment.ContentRootPath, "certs", "lab-cert.pfx");
const string certPassword = "operalegacylab-test-only"; // throwaway local test cert, not a secret worth protecting

builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(IPAddress.Any, port, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1;
    });

    options.Listen(IPAddress.Any, httpsPort, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1;
        var cert = DevCertificate.GetOrCreate(certPath, certPassword);
        // No custom SslProtocols here: this listener's only client is ngrok's
        // own agent (a modern Go binary), not the phone, so Kestrel's normal
        // modern-TLS defaults are exactly right - nothing legacy to support.
        listenOptions.UseHttps(cert);
    });

    // Old/slow mobile links can take a long time to send or receive a request -
    // don't let Kestrel's default minimum-data-rate watchdogs kill those
    // connections early.
    options.Limits.MinRequestBodyDataRate = null;
    options.Limits.MinResponseDataRate = null;
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(2);
});

builder.Services.AddSingleton<LabSessionStore>();
builder.Services.AddSingleton<DeviceResultLog>();
builder.Services.AddRazorPages();
builder.Services.AddHttpClient("uaprof", client =>
{
    client.Timeout = TimeSpan.FromSeconds(5);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("OperaLegacyLab/1.0 (+capability-test-harness)");
});

var app = builder.Build();

// Force DeviceResultLog to construct now rather than lazily on the first
// request that needs it, so any leftover device-results.txt from a
// previous run is cleared immediately on startup, not left stale until
// the first test happens to run.
app.Services.GetRequiredService<DeviceResultLog>();

// ngrok terminates the real TLS connection with the phone and forwards to us
// (over its own separate local connection - http or https depending on which
// port you pointed it at) - without this, diagnostic pages could wrongly
// report "http" even though the phone connected over https. KnownNetworks/
// KnownProxies are cleared because ngrok's edge IP can't be listed in
// advance; that's a deliberate relaxation appropriate for a throwaway
// diagnostic tool, not a pattern to copy into anything security-sensitive.
var forwardedHeaderOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
};
forwardedHeaderOptions.KnownIPNetworks.Clear();
forwardedHeaderOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeaderOptions);

// Deliberately no app.UseHttpsRedirection() / no HSTS: what matters here is
// ngrok's own https to the phone, not redirect behavior on this local hop.
app.UseStaticFiles();

// Buffers every response and, only for pages using the shared _Layout.cshtml, prepends a real
// page-size/compression report and (unlike anywhere else in this app) actually compresses the
// response when this request's own Accept-Encoding asked for gzip - see PageSizeReportMiddleware's
// own doc comment for why this is safe for the pages that deliberately bypass the shared layout for
// byte-exact testing (EncodingLatin1, WapWml, ReportText, /test/qr-png, every /test/compression/*).
app.UseMiddleware<PageSizeReportMiddleware>();

// Every route in this app - home page, every test, diagnostics, the report -
// is a Razor Page under Pages/, routed by convention/@page directive. This
// one call replaces what used to be a dozen separate app.MapXxxEndpoints()
// registrations.
app.MapRazorPages();

app.Run();
