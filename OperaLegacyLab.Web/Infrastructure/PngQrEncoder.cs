using System.IO.Compression;
using QRCoder;

namespace OperaLegacyLab.Web.Infrastructure;

/// <summary>
/// Renders a QRCoder module matrix as a real PNG image, built entirely from BCL primitives -
/// System.IO.Compression.ZLibStream for the (correctly, standard-)compressed IDAT payload, a
/// hand-rolled CRC32 for chunk checksums - no System.Drawing, no SkiaSharp, no ImageSharp, so this
/// has the same "no extra dependency" property as the vendored QRCoder encoder itself.
///
/// This exists because the HTML-table renderer (HtmlTableQrRenderer), while it did fix the
/// "QR never appears at all" problem from the original one-&lt;img&gt;-per-module version, turned
/// out to render as a visibly non-square rectangle on the actual device - since confirmed (see
/// HtmlTableQrRenderer's doc comment) to be a &lt;td&gt;'s declared height being only a minimum, with
/// &amp;nbsp; at normal font size forcing every row about 3x taller than declared regardless of the
/// declared pixel width. A single real image sidesteps that category of problem entirely: pixels are
/// pixels, not a grid the browser has to lay out itself.
/// </summary>
public static class PngQrEncoder
{
    public static byte[] Encode(QRCodeData data, int pixelsPerModule)
    {
        var matrix = data.ModuleMatrix;
        int modules = matrix.Count;
        int size = modules * pixelsPerModule;

        // Raw (uncompressed) scanline data: each row is a leading filter-type byte (0 = None)
        // followed by one grayscale byte per pixel (8-bit depth - simpler and more robust to hand-roll
        // correctly than 1-bit-packed scanlines, and the up-scaled solid blocks every module becomes
        // compress extremely well anyway).
        var raw = new byte[size * (size + 1)];
        for (int y = 0; y < size; y++)
        {
            int moduleY = y / pixelsPerModule;
            var row = matrix[moduleY];
            int rowStart = y * (size + 1);
            raw[rowStart] = 0; // filter type: None

            for (int x = 0; x < size; x++)
            {
                int moduleX = x / pixelsPerModule;
                raw[rowStart + 1 + x] = row[moduleX] ? (byte)0 : (byte)255;
            }
        }

        byte[] zlibData;
        using (var ms = new MemoryStream())
        {
            using (var zlib = new ZLibStream(ms, CompressionLevel.Optimal, leaveOpen: true))
                zlib.Write(raw, 0, raw.Length);
            zlibData = ms.ToArray();
        }

        using var png = new MemoryStream();
        png.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }); // PNG signature

        var ihdr = new byte[13];
        WriteUInt32BE(ihdr, 0, (uint)size);  // width
        WriteUInt32BE(ihdr, 4, (uint)size);  // height
        ihdr[8] = 8;  // bit depth
        ihdr[9] = 0;  // color type: grayscale
        ihdr[10] = 0; // compression method
        ihdr[11] = 0; // filter method
        ihdr[12] = 0; // interlace method
        WriteChunk(png, "IHDR", ihdr);

        WriteChunk(png, "IDAT", zlibData);
        WriteChunk(png, "IEND", Array.Empty<byte>());

        return png.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, byte[] payload)
    {
        Span<byte> lengthBuf = stackalloc byte[4];
        WriteUInt32BE(lengthBuf, 0, (uint)payload.Length);
        stream.Write(lengthBuf);

        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        stream.Write(typeBytes);
        stream.Write(payload);

        var crc = Crc32(typeBytes, payload);
        Span<byte> crcBuf = stackalloc byte[4];
        WriteUInt32BE(crcBuf, 0, crc);
        stream.Write(crcBuf);
    }

    private static void WriteUInt32BE(Span<byte> dest, int offset, uint value)
    {
        dest[offset] = (byte)(value >> 24);
        dest[offset + 1] = (byte)(value >> 16);
        dest[offset + 2] = (byte)(value >> 8);
        dest[offset + 3] = (byte)value;
    }

    private static readonly uint[] Crc32Table = BuildCrc32Table();

    private static uint[] BuildCrc32Table()
    {
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint c = i;
            for (int k = 0; k < 8; k++)
                c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            table[i] = c;
        }
        return table;
    }

    /// <summary>Standard PNG chunk CRC32 (IEEE 802.3 polynomial), computed over type+payload together.</summary>
    private static uint Crc32(byte[] type, byte[] payload)
    {
        uint crc = 0xFFFFFFFF;
        foreach (var b in type)
            crc = Crc32Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
        foreach (var b in payload)
            crc = Crc32Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return crc ^ 0xFFFFFFFF;
    }
}
