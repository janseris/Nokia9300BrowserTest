using OperaLegacyLab.Web.Infrastructure;

namespace OperaLegacyLab.Web.Pages.Test;

public class WapModel : LabPageModel
{
    public WapModel(LabSessionStore store) : base(store) { }

    public string Accept { get; private set; } = "";
    public bool AdvertisesWml { get; private set; }

    public void OnGet()
    {
        ResolveLab("WAP / WML test");
        Accept = Request.Headers.Accept.ToString();
        AdvertisesWml = Accept.Contains("vnd.wap.wml", StringComparison.OrdinalIgnoreCase);
    }
}
