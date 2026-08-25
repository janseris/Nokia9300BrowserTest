using Microsoft.AspNetCore.Mvc;
using OperaLegacyLab.Web.Infrastructure;
using static OperaLegacyLab.Web.Infrastructure.TableVariantRenderer;

namespace OperaLegacyLab.Web.Pages.Test;

/// <summary>
/// Follow-up to /test/qr-table: that page's own production technique (table-layout:fixed + colgroup +
/// a font-size:1px;line-height:1px style, see HtmlTableQrRenderer's own doc comment for the full history)
/// was confirmed fixed on the Nokia 9300's native Opera 6, but a Nokia 5130 running Opera Mini 4.5 - a
/// different phone AND a fundamentally different rendering path (Opera Mini pre-renders/transcodes pages
/// on Opera's own servers rather than laying them out on the handset itself, unlike Opera 6's native
/// on-device engine) - still shows the QR table as vertically stretched. Rather than guessing at a
/// single new fix and burning another round-trip if it's wrong too, this page puts several independently
/// labeled variants on one page, all built from the same TableVariantRenderer, so one visit to the phone
/// can report on all of them at once.
///
/// Variant 2 deliberately REPRODUCES the exact current production technique (not a new attempt) - it's
/// the control that confirms this test page is actually reproducing the same bug already seen on the
/// real QR page, not some unrelated rendering quirk of a differently-built test page.
///
/// Originally six variants, numbered 1-6. Variant 1 ("no fixes at all" - plain HTML 3.2/4.01 attributes,
/// no style attribute anywhere) was removed after it turned out to render stretched even in a real desktop
/// browser (Firefox on PC) rather than just the legacy/mobile targets this lab exists to test - it wasn't
/// telling us anything about old-phone-specific rendering, just confirming that unstyled attribute-only
/// sizing is broken everywhere, which every other variant already assumes. The remaining five variants
/// keep their ORIGINAL numbers (2-6) rather than being renumbered down to 1-5, so "Variant 4" keeps meaning
/// the same thing here as it does in every place that already refers to it by that number (see
/// QrRenderPreference.cs/QrRenderMode.TableEmptyCells in ResidentPass.Web.RazorPages, which cites this
/// page's Variant 4 by name as the confirmed fix for the Nokia 5130/Opera Mini 4.5).
/// </summary>
public class TableVariantsModel : LabPageModel
{
    private const int Modules = 12; // kept small - see BuildVariants' own remarks and TableVariantRenderer's on why a bigger, easier-to-see grid would actually hide the bug; also keeps all five variants' worth of markup light on a slow mobile link
    private const int PxPerModule = 6; // matches QrTestSettings.PixelsPerModule - see TableVariantRenderer's own remarks on why this must stay small

    private readonly DeviceResultLog _deviceLog;

    public TableVariantsModel(LabSessionStore store, DeviceResultLog deviceLog) : base(store) => _deviceLog = deviceLog;

    public IReadOnlyList<(Spec Spec, string Html)> Variants { get; private set; } = Array.Empty<(Spec, string)>();

    public void OnGet()
    {
        ResolveLab("Table cell sizing variants test");
        BuildVariants();
    }

    public IActionResult OnPost()
    {
        ResolveLab("Table cell sizing variants test");
        var (result, note) = SelfReport.ReadForm(Request.Form);
        Lab.SelfReports["table_variants"] = result;
        Lab.SelfReportNotes["table_variants"] = note;
        _deviceLog.Record(HttpContext, "table_variants", "Table cell sizing variants", result, note);
        return Redirect(Markup.U("/report", Lab.Code));
    }

    private void BuildVariants()
    {
        // Variant 1 ("no fixes at all") used to head this list - removed, see this class's own remarks
        // above on why (failed even on desktop Firefox, not a legacy-browser-specific finding). The
        // remaining variants below deliberately keep their original numbers 2-6 rather than being
        // renumbered, for the same reason.
        var specs = new List<Spec>
        {
            new Spec(
                Label: "Variant 2 - current production technique",
                Description: "Exactly what HtmlTableQrRenderer uses today on the real QR pages: table-layout:fixed + an explicit <colgroup> + style=\"font-size:1px;line-height:1px\" on the table + &nbsp; in every cell. This is the CONTROL - it should look exactly like the real QR code already reported stretched, confirming this page reproduces the same bug rather than a different one.",
                FixedLayout: true, Colgroup: true, IncludeStyleAttr: true, ExtraTableStyle: "font-size:1px;line-height:1px",
                Content: CellContent.Nbsp),

            new Spec(
                Label: "Variant 3 - line-height:0 instead of font-size:1px",
                Description: "Same as Variant 2, but the table's own style swaps font-size:1px;line-height:1px for line-height:0 alone - a font at 1px can still carry its own ascent/descent metrics into the line box on some engines; a flat line-height:0 leaves nothing to collapse.",
                FixedLayout: true, Colgroup: true, IncludeStyleAttr: true, ExtraTableStyle: "line-height:0",
                Content: CellContent.Nbsp),

            new Spec(
                Label: "Variant 4 - empty cells instead of &nbsp;",
                Description: "Same as Variant 2 (table-layout:fixed + colgroup + font-size:1px;line-height:1px), but every <td></td> is left completely empty instead of holding an &nbsp; - isolates whether the nbsp character's own line box, not the table layout algorithm, is what's actually forcing the extra height.",
                FixedLayout: true, Colgroup: true, IncludeStyleAttr: true, ExtraTableStyle: "font-size:1px;line-height:1px",
                Content: CellContent.Empty),

            new Spec(
                Label: "Variant 5 - spacer-GIF sizing",
                Description: "Every cell holds a 1x1 transparent GIF stretched via its own width/height attributes to fill the module exactly, instead of any text/whitespace - the classic pre-CSS \"spacer.gif\" technique. table-layout:fixed + colgroup kept as belt-and-braces, but no font-size/line-height style at all - an image's own box shouldn't need it.",
                FixedLayout: true, Colgroup: true, IncludeStyleAttr: true, ExtraTableStyle: null,
                Content: CellContent.SpacerImage),

            new Spec(
                Label: "Variant 6 - spacer-GIF, no other CSS help at all",
                Description: "Same spacer-GIF cell content as Variant 5, but with none of the CSS scaffolding: no table-layout:fixed, no colgroup, no style attribute anywhere - just the plain width/height/bgcolor attributes every variant here starts from, plus the image. Tests whether the image alone is enough once every other technique is stripped back out.",
                FixedLayout: false, Colgroup: false, IncludeStyleAttr: false, ExtraTableStyle: null,
                Content: CellContent.SpacerImage),
        };

        Variants = specs.Select(s => (s, TableVariantRenderer.Render(Modules, PxPerModule, s))).ToList();
    }
}
