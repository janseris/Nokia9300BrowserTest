using OperaLegacyLab.Web.Infrastructure;

namespace OperaLegacyLab.Web.Pages;

public class IndexModel : LabPageModel
{
    public IndexModel(LabSessionStore store) : base(store) { }

    public bool IsHttps { get; private set; }

    public void OnGet()
    {
        ResolveLab("Opera Legacy Lab");
        IsHttps = HttpContext.Request.IsHttps;
    }
}
