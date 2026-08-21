using OperaLegacyLab.Web.Infrastructure;

namespace OperaLegacyLab.Web.Pages.Test;

public class FramesViewModel : LabPageModel
{
    public FramesViewModel(LabSessionStore store) : base(store) { }

    public string NavUrl { get; private set; } = "";
    public string ContentUrl { get; private set; } = "";

    public void OnGet()
    {
        ResolveLab("Frames test");
        NavUrl = Markup.U("/test/frames/nav", Lab.Code);
        ContentUrl = Markup.U("/test/frames/content", Lab.Code);
    }
}
