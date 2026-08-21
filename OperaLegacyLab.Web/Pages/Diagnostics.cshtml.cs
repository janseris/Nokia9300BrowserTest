using OperaLegacyLab.Web.Infrastructure;

namespace OperaLegacyLab.Web.Pages;

public class DiagnosticsModel : LabPageModel
{
    public DiagnosticsModel(LabSessionStore store) : base(store) { }

    public string UaProfUrl => Lab.WapProfileUrl ?? "";
    public bool HasUaProf => !string.IsNullOrEmpty(Lab.WapProfileUrl);

    public void OnGet()
    {
        ResolveLab("Diagnostics");
    }
}
