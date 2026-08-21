using System.Security.Cryptography;

namespace OperaLegacyLab.Web.Infrastructure;

/// <summary>
/// A small, self-contained RFC 6238 TOTP generator (HMAC-SHA1, 6 digits,
/// 15-second period by default) for the /test/qr feasibility test.
///
/// This intentionally does NOT reference the separate ResidentPass.MAUI
/// solution's own QrCodeGenerator project - OperaLegacyLab is a standalone
/// diagnostic lab with no dependency on that app's code or secrets. The
/// algorithm is the same standard RFC 6238 math (and happens to default to
/// the same 15-second period ResidentPass uses), which is exactly why this
/// test is a meaningful proxy for "could a browser like this show a
/// ResidentPass-style rotating QR code" without touching anything from that
/// other project.
/// </summary>
public static class Totp
{
    public static string Compute(byte[] secret, DateTimeOffset utcNow, int periodSeconds = 15, int digits = 6)
    {
        long counter = utcNow.ToUnixTimeSeconds() / periodSeconds;

        var counterBytes = new byte[8];
        for (int i = 7; i >= 0; i--)
        {
            counterBytes[i] = (byte)(counter & 0xFF);
            counter >>= 8;
        }

        using var hmac = new HMACSHA1(secret);
        byte[] hash = hmac.ComputeHash(counterBytes);

        int offset = hash[^1] & 0x0F;
        int binary = ((hash[offset] & 0x7F) << 24)
                   | ((hash[offset + 1] & 0xFF) << 16)
                   | ((hash[offset + 2] & 0xFF) << 8)
                   | (hash[offset + 3] & 0xFF);

        int modulo = (int)Math.Pow(10, digits);
        return (binary % modulo).ToString().PadLeft(digits, '0');
    }

    /// <summary>Seconds remaining in the current TOTP step.</summary>
    public static int SecondsRemaining(DateTimeOffset utcNow, int periodSeconds = 15)
    {
        long seconds = utcNow.ToUnixTimeSeconds();
        return periodSeconds - (int)(seconds % periodSeconds);
    }

    /// <summary>Lazily creates and caches a random per-session secret on first use.</summary>
    public static byte[] GetOrCreateSecret(LabSession lab)
    {
        if (lab.QrTotpSecret is null)
        {
            var secret = new byte[20]; // 160 bits, same size OtpNet/authenticator apps typically use
            RandomNumberGenerator.Fill(secret);
            lab.QrTotpSecret = secret;
        }
        return lab.QrTotpSecret;
    }
}
