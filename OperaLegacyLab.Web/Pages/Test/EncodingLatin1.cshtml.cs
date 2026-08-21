using System.Text;
using Microsoft.AspNetCore.Mvc;
using OperaLegacyLab.Web.Infrastructure;

namespace OperaLegacyLab.Web.Pages.Test;

public class EncodingLatin1Model : LabPageModel
{
    public EncodingLatin1Model(LabSessionStore store) : base(store) { }

    // Razor's own view-rendering pipeline always writes UTF-8 bytes to the
    // response regardless of what Content-Type header text you set - it has
    // no notion of "render this page as ISO-8859-1". Since the entire point
    // of this page is testing genuine ISO-8859-1 BYTES (not a UTF-8 body
    // mislabeled with an ISO-8859-1 header), OnGet bypasses the .cshtml view
    // entirely and returns a ContentResult built with the real encoding -
    // this page still exists purely so ngrok/routing sees it as an ordinary
    // Razor Page (no app.MapXxx call anywhere), it just never renders its
    // own markup.
    public IActionResult OnGet()
    {
        ResolveLab("ISO-8859-1 sample");

        var sample = EncodingUtf8Model.SampleText
            .Replace("€", "EUR")
            .Replace("“", "\"")
            .Replace("”", "\"");
        // ISO-8859-1 cannot represent U+20AC (euro) or curly quotes; substituted above.

        // Deliberately NOT run through Markup.Escape here: WebUtility.HtmlEncode
        // numeric-entity-escapes non-ASCII characters too (e.g. "&#233;"
        // instead of a literal single ISO-8859-1 byte for e-acute), which
        // would make this page pass even on a browser with no real Latin-1
        // decoding - ancient browsers resolve numeric character references
        // regardless of the page's declared charset. sample is a hardcoded
        // constant with no HTML metacharacters, so this is safe.

        var html = $"""
                    <!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.01 Transitional//EN" "http://www.w3.org/TR/html4/loose.dtd">
                    <html>
                    <head><title>ISO-8859-1 sample</title></head>
                    <body>
                    <h1>ISO-8859-1 encoded sample</h1>
                    <p>Content-Type header sent: <tt>text/html; charset=iso-8859-1</tt></p>
                    <p>{sample}</p>
                    <p><i>Note: the euro sign and curly quotes aren't representable in
                    ISO-8859-1 and are shown as plain ASCII substitutes above on purpose -
                    Latin-1 genuinely cannot encode them.</i></p>
                    <hr>
                    <p>
                    <a href="/?lab={Lab.Code}">Home</a> |
                    <a href="/report?lab={Lab.Code}">View report</a>
                    </p>
                    <p><font size="1">Lab session: {Markup.Escape(Lab.Code)}</font></p>
                    </body>
                    </html>
                    """;

        return Content(html, "text/html", Encoding.GetEncoding("iso-8859-1"));
    }
}
