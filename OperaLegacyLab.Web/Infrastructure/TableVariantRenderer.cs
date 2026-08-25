namespace OperaLegacyLab.Web.Infrastructure;

/// <summary>
/// Renders a small black/white checkerboard as a plain HTML &lt;table&gt;, the same way
/// HtmlTableQrRenderer renders a real QR code - but parameterized so /test/table-variants can render
/// several DIFFERENT combinations of the table-sizing techniques tried there, side by side, to find out
/// which one (if any) actually keeps a &lt;td&gt; at its declared width/height on a browser where the
/// "production" combination (table-layout:fixed + colgroup + font-size:1px;line-height:1px, see
/// HtmlTableQrRenderer's own doc comment) still renders as a visibly stretched rectangle - reported on a
/// Nokia 5130 running Opera Mini 4.5, a genuinely different rendering path from the Nokia 9300's native
/// Opera 6 (Opera Mini pre-renders/transcodes server-side rather than laying the page out on the handset
/// itself), so a fix confirmed on one is not automatically assumed to hold on the other.
///
/// A checkerboard (every cell its own color, alternating) rather than run-length-encoded blocks like a
/// real QR - here every module needs to be independently visible so a vertically-stretched row shows up
/// immediately as a tall thin rectangle instead of a small square, rather than being hidden inside one
/// large same-color merged run the way a QR's own quiet zone would.
///
/// pxPerModule is deliberately kept SMALL (matching QrTestSettings.PixelsPerModule, not some larger,
/// easier-to-see value) - the whole bug being chased only shows up when the declared cell height is
/// smaller than an "&amp;nbsp;"'s own line-box height (about 18-19px at normal font size, per
/// HtmlTableQrRenderer's own measurement). A comfortably large module size would hide the exact bug
/// this test exists to reproduce.
/// </summary>
public static class TableVariantRenderer
{
    public enum CellContent
    {
        /// <summary>"&amp;nbsp;" inside every &lt;td&gt; - the production HtmlTableQrRenderer's own
        /// choice, there specifically as a defensive "something has to be in here so a browser that
        /// collapses a truly empty cell to zero size doesn't do that" measure.</summary>
        Nbsp,

        /// <summary>A genuinely empty &lt;td&gt;&lt;/td&gt; - tests whether nbsp's own line box is
        /// actually the thing forcing the extra height, by removing it while keeping every other
        /// technique the same.</summary>
        Empty,

        /// <summary>A classic "spacer.gif" &lt;img&gt;, stretched via its own width/height attributes to
        /// exactly fill the module - the oldest, most browser-agnostic way to force a box to an exact
        /// pixel size that isn't just it a raw HTML/CSS attribute, since it doesn't depend on the table
        /// layout algorithm OR font metrics at all: an image's own intrinsic box size is what's being
        /// stretched, not a line of text sitting inside one.</summary>
        SpacerImage,
    }

    public sealed record Spec(
        string Label,
        string Description,
        bool FixedLayout,
        bool Colgroup,
        bool IncludeStyleAttr,
        string? ExtraTableStyle,
        CellContent Content);

    public static string Render(int modules, int pxPerModule, Spec spec)
    {
        int totalSize = modules * pxPerModule;
        var sb = new System.Text.StringBuilder(modules * modules * 48);

        sb.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" bgcolor=\"#ffffff\"")
          .Append(" width=\"").Append(totalSize).Append('"')
          .Append(" height=\"").Append(totalSize).Append('"');

        if (spec.IncludeStyleAttr)
        {
            var styleParts = new List<string>();
            if (spec.FixedLayout) styleParts.Add("table-layout:fixed");
            styleParts.Add($"width:{totalSize}px");
            styleParts.Add($"height:{totalSize}px");
            if (!string.IsNullOrEmpty(spec.ExtraTableStyle)) styleParts.Add(spec.ExtraTableStyle);
            sb.Append(" style=\"").Append(string.Join(';', styleParts)).Append('"');
        }
        sb.Append('>');

        if (spec.Colgroup)
        {
            sb.Append("<colgroup>");
            for (int c = 0; c < modules; c++)
                sb.Append("<col width=\"").Append(pxPerModule).Append("\">");
            sb.Append("</colgroup>");
        }

        for (int y = 0; y < modules; y++)
        {
            sb.Append("<tr height=\"").Append(pxPerModule).Append("\">");
            for (int x = 0; x < modules; x++)
            {
                bool dark = (x + y) % 2 == 0;
                string color = dark ? "#000000" : "#ffffff";

                string inner = spec.Content switch
                {
                    CellContent.Nbsp => "&nbsp;",
                    CellContent.Empty => "",
                    CellContent.SpacerImage =>
                        $"<img src=\"/img/qr-spacer.gif\" width=\"{pxPerModule}\" height=\"{pxPerModule}\" alt=\"\" border=\"0\">",
                    _ => "&nbsp;",
                };

                sb.Append("<td width=\"").Append(pxPerModule).Append('"')
                  .Append(" height=\"").Append(pxPerModule).Append('"')
                  .Append(" bgcolor=\"").Append(color).Append("\">").Append(inner).Append("</td>");
            }
            sb.Append("</tr>");
        }

        sb.Append("</table>");
        return sb.ToString();
    }
}
