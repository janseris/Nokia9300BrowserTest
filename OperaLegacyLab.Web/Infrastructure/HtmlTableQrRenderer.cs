using QRCoder;

namespace OperaLegacyLab.Web.Infrastructure;

/// <summary>
/// Renders a QRCoder module matrix as a plain HTML &lt;table&gt;, colored black/white with the
/// bgcolor ATTRIBUTE (not CSS background-color) and sized with the width/height ATTRIBUTES directly
/// on each &lt;td&gt; (not CSS, not a spacer &lt;img&gt;) - all HTML 3.2/4.01-era table idioms, so
/// this renders identically whether or not the browser has any CSS support at all.
///
/// First real-device run of the original one-&lt;img&gt;-per-module version of this renderer showed
/// the code/countdown text updating correctly but the QR table itself never appearing at all on the
/// actual Nokia 9300 - almost certainly the ~800-1700 extra same-origin image requests (one per
/// module) overwhelming a 2001-era phone's request queue/memory, not a rendering-correctness
/// problem (see git history for that version). This version removes the &lt;img&gt; entirely -
/// width/height attributes on &lt;td&gt; need no accompanying image to take effect - and run-length
/// encodes each row (consecutive same-color modules become one &lt;td colspan="N" width="N*px"&gt;)
/// so the huge uniform blocks every QR code has (quiet zone, finder patterns) collapse to a handful
/// of cells instead of one per module. A quiet, all-white row - four of them, top and bottom - now
/// costs exactly one &lt;td&gt; instead of one per module. An empty &lt;td&gt; would rely on the
/// browser respecting width/height with no content to size against; &amp;nbsp; inside every cell is
/// a defensive belt-and-braces against a browser that doesn't.
///
/// No custom finder-pattern geometry is needed here (contrast ResidentPass.MAUI's
/// RoundedQrSvgRenderer, which draws the three finder "eyes" separately as rounded shapes): every
/// module - finder patterns, timing patterns, quiet zone, data - comes straight out of QRCoder's own
/// ModuleMatrix and is rendered as plain rectangular cells, so the standard QR geometry (and the
/// run-length merging above) both fall out automatically with no version-specific logic.
///
/// The height="N" attribute on a &lt;td&gt; is only a MINIMUM - a browser still grows the row to fit
/// whatever's inside it, and &amp;nbsp; at a normal ~16px font has a line box around 18-19px tall
/// regardless of what height says. Measured directly in headless Chromium: a cell declared
/// width="42" height="6" actually rendered as 42x18, not 42x6 - width tracks the declared pixels
/// (nothing inside a &lt;td&gt; normally forces it wider than its content needs), but height was
/// almost 3x too tall, purely from that one &amp;nbsp; character's line height. Every module ends up
/// exactly as wide as it should be and about 3x too tall - a visibly stretched rectangle.
///
/// First attempted fix: shrink the font/line-height for the table's content to near-zero via a single
/// style="font-size:1px;line-height:1px" on the &lt;table&gt; element, relying on ordinary CSS
/// inheritance to reach every &lt;td&gt; without repeating it per cell. Verified square in headless
/// Chromium - but on the real Nokia 9300/Opera 6 it made no difference at all, still visibly
/// stretched. Most likely Opera 6 either doesn't apply CSS from a style="..." attribute the way a
/// modern browser does, or table row-height sizing there simply isn't driven by font metrics the same
/// way, so shrinking the font changed nothing it was actually keying off.
///
/// Current fix, aimed specifically at very old browsers' table-layout ALGORITHM rather than at the
/// font: table-layout:fixed (CSS2, but a long-established, non-exotic property - unlike relying on
/// font-metrics inheritance, this tells the layout engine itself to stop measuring cell CONTENT at
/// all for sizing, both width and height, and just use the declared attributes/CSS dimensions
/// directly - which is exactly the "ignore content, trust the numbers" behavior needed here). Combined
/// with an explicit &lt;colgroup&gt; of one &lt;col width="pixelsPerModule"&gt; per module column:
/// under table-layout:fixed a browser is only supposed to look at the FIRST row (or an explicit
/// colgroup) to learn column widths, and this table's first row is nearly always a single run-length
/// merged &lt;td colspan="N"&gt; for the top quiet zone - without an explicit colgroup that would read
/// as "one column, full width" and misalign every other row's narrower cells against it. The colgroup
/// removes that ambiguity regardless of how any individual row happens to run-length-encode. The
/// table's own width/height are set both ways (width="W" height="H" attributes AND
/// style="width:Wpx;height:Hpx") for the same belt-and-braces reason every other piece of markup here
/// uses two ways of saying the same thing - whichever one a given browser actually honors. The earlier
/// font-size/line-height style is left in place too (harmless, and still helps modern browsers reach
/// the exact pixel size via a second, independent path) and every &lt;tr&gt; also carries its own
/// height="N" attribute, since old engines were inconsistent about which of &lt;tr&gt;/&lt;td&gt;
/// height they actually read.
/// </summary>
public static class HtmlTableQrRenderer
{
    public static string Render(QRCodeData data, int pixelsPerModule = 6)
    {
        var matrix = data.ModuleMatrix;
        int numModules = matrix.Count;
        int totalSize = numModules * pixelsPerModule;

        var sb = new System.Text.StringBuilder(numModules * 40);
        sb.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" bgcolor=\"#ffffff\"")
          .Append(" width=\"").Append(totalSize).Append('"')
          .Append(" height=\"").Append(totalSize).Append('"')
          .Append(" style=\"table-layout:fixed;width:").Append(totalSize).Append("px;height:").Append(totalSize)
          .Append("px;font-size:1px;line-height:1px\">");

        sb.Append("<colgroup>");
        for (int c = 0; c < numModules; c++)
            sb.Append("<col width=\"").Append(pixelsPerModule).Append("\">");
        sb.Append("</colgroup>");

        for (int y = 0; y < numModules; y++)
        {
            sb.Append("<tr height=\"").Append(pixelsPerModule).Append("\">");
            var row = matrix[y];

            int x = 0;
            while (x < numModules)
            {
                bool dark = row[x];
                int runStart = x;
                while (x < numModules && row[x] == dark) x++;
                int runLength = x - runStart;

                string color = dark ? "#000000" : "#ffffff";
                int width = runLength * pixelsPerModule;

                sb.Append("<td");
                if (runLength > 1)
                    sb.Append(" colspan=\"").Append(runLength).Append('"');
                sb.Append(" width=\"").Append(width).Append('"')
                  .Append(" height=\"").Append(pixelsPerModule).Append('"')
                  .Append(" bgcolor=\"").Append(color).Append("\">&nbsp;</td>");
            }

            sb.Append("</tr>");
        }

        sb.Append("</table>");
        return sb.ToString();
    }
}
