namespace OperaLegacyLab.Web.Infrastructure;

/// <summary>
/// Shared constants for the QR feasibility tests (/test/qr-image, /test/qr-table, and the
/// /test/qr-png resource they both ultimately read from). Split out of the old combined QrModel so
/// neither of the two now-separate test pages needs to reference the other's class just to share a
/// couple of numbers.
/// </summary>
public static class QrTestSettings
{
    public const int PeriodSeconds = 15;
    public const int PixelsPerModule = 6;

    // /test/qr-table targets this as a hard cap on its rendered height (screens this lab targets are
    // as small as 640x200) - QrTableModel picks pixels-per-module as floor(TableMaxHeightPx /
    // moduleCount) so the table's actual height (moduleCount * pixelsPerModule) never exceeds this,
    // however many modules the current payload happens to need.
    public const int TableMaxHeightPx = 180;
}
