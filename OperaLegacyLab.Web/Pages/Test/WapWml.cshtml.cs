using System.Text;
using Microsoft.AspNetCore.Mvc;
using OperaLegacyLab.Web.Infrastructure;

namespace OperaLegacyLab.Web.Pages.Test;

public class WapWmlModel : LabPageModel
{
    public WapWmlModel(LabSessionStore store) : base(store) { }

    // Same rationale as EncodingLatin1: this bypasses the .cshtml view engine
    // entirely, both because the content type isn't HTML and because WML is
    // strict XML that must be built byte-for-byte, not run through Razor's
    // HTML-oriented encoding.
    public IActionResult OnGet()
    {
        ResolveLab("WML card");
        Lab.WmlRequestAccept = Request.Headers.Accept.ToString();

        // WML is XML, so unlike ordinary HTML, "&" inside an attribute value
        // MUST be escaped as "&amp;" or the whole card is not well-formed XML.
        var yes = (Markup.U("/test/wap/result", Lab.Code) + "&ok=1").Replace("&", "&amp;");
        var no = (Markup.U("/test/wap/result", Lab.Code) + "&ok=0").Replace("&", "&amp;");

        var wml = $"""
                   <?xml version="1.0"?>
                   <!DOCTYPE wml PUBLIC "-//WAPFORUM//DTD WML 1.3//EN" "http://www.wapforum.org/DTD/wml13.dtd">
                   <wml>
                   <card id="card1" title="WML Test">
                   <p>
                   If this reads as a styled WAP card - not garbled XML tags - your browser
                   rendered WML successfully.
                   </p>
                   <p>
                   <a href="{yes}">Yes, this rendered as a card</a><br/>
                   <a href="{no}">No, this looked broken</a>
                   </p>
                   </card>
                   </wml>
                   """;
        return Content(wml, "text/vnd.wap.wml; charset=utf-8", Encoding.UTF8);
    }
}
