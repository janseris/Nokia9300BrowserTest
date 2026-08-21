using OperaLegacyLab.Web.Infrastructure;

namespace OperaLegacyLab.Web.Pages.Test;

public class EncodingUtf8Model : LabPageModel
{
    public EncodingUtf8Model(LabSessionStore store) : base(store) { }

    // Deliberately includes characters outside plain ASCII so a mismatch
    // between declared charset and what the browser actually decodes shows
    // up clearly.
    public const string SampleText = "e with acute: é, u umlaut: ü, n with tilde: ñ, " +
                                      "sharp s: ß, euro sign: €, pound: £, " +
                                      "em dash: —, smart quote: “quoted”";

    public void OnGet()
    {
        ResolveLab("UTF-8 sample");
        Response.ContentType = "text/html; charset=utf-8";
    }
}
