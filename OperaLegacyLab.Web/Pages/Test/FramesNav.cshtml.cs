using OperaLegacyLab.Web.Infrastructure;

namespace OperaLegacyLab.Web.Pages.Test;

public class FramesNavModel : LabPageModel
{
    public FramesNavModel(LabSessionStore store) : base(store) { }

    public string AltUrl { get; private set; } = "";

    public void OnGet()
    {
        ResolveLab("Nav frame");
        AltUrl = Markup.U("/test/frames/content2", Lab.Code);
    }
}
