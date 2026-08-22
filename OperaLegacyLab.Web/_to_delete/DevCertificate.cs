using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace OperaLegacyLab.Web.Infrastructure;

/// <summary>
/// RETIRED - no longer referenced by Program.cs, safe to delete this file (and any old
/// certs/lab-cert.pfx next to it, already gitignored either way).
///
/// This used to generate a one-off self-signed RSA certificate for the app's local HTTPS listener.
/// That worked fine for ngrok, which doesn't verify the origin certificate by default, but a
/// certificate-verifying tunnel client (Cloudflare Tunnel/cloudflared does verify, by default) will
/// always reject an unfamiliar self-signed cert like this one - there's no CA chain or trust-store entry
/// backing it. Program.cs now instead points its HTTPS listener at the standard ASP.NET Core HTTPS
/// DEVELOPMENT certificate via a bare `listenOptions.UseHttps()` (no certificate argument), which
/// `dotnet dev-certs https --trust` (run once, locally) can actually add to the OS trust store - fixing
/// exactly the "certificate isn't trusted" problem this class's approach ran into with cloudflared.
/// </summary>
public static class DevCertificate
{
    public static X509Certificate2 GetOrCreate(string path, string password)
    {
        if (File.Exists(path))
        {
            try
            {
                return X509CertificateLoader.LoadPkcs12FromFile(path, password,
                    X509KeyStorageFlags.Exportable);
            }
            catch
            {
                // Fall through and regenerate if the cached file is unreadable/corrupt.
            }
        }

        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest(
            "CN=OperaLegacyLab",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        req.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
        req.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") /* server auth */ }, false));

        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName("localhost");
        sanBuilder.AddIpAddress(IPAddress.Loopback);
        try { sanBuilder.AddDnsName(Environment.MachineName); } catch { /* ignore */ }
        foreach (var ip in GetLocalIPv4Addresses())
            sanBuilder.AddIpAddress(ip);
        req.CertificateExtensions.Add(sanBuilder.Build());

        using var cert = req.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(5));

        var pfxBytes = cert.Export(X509ContentType.Pfx, password);

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, pfxBytes);

        return X509CertificateLoader.LoadPkcs12(pfxBytes, password, X509KeyStorageFlags.Exportable);
    }

    /// <summary>
    /// Every non-loopback IPv4 address currently assigned to this machine, so the
    /// certificate covers whatever address ngrok (or anything else local) uses.
    /// </summary>
    private static IEnumerable<IPAddress> GetLocalIPv4Addresses()
    {
        var seen = new HashSet<IPAddress>();
        NetworkInterface[] interfaces;
        try { interfaces = NetworkInterface.GetAllNetworkInterfaces(); }
        catch { yield break; }

        foreach (var nic in interfaces)
        {
            if (nic.OperationalStatus != OperationalStatus.Up) continue;
            IPInterfaceProperties props;
            try { props = nic.GetIPProperties(); } catch { continue; }

            foreach (var addrInfo in props.UnicastAddresses)
            {
                var addr = addrInfo.Address;
                if (addr.AddressFamily != AddressFamily.InterNetwork) continue;
                if (IPAddress.IsLoopback(addr)) continue;
                if (seen.Add(addr)) yield return addr;
            }
        }
    }
}
