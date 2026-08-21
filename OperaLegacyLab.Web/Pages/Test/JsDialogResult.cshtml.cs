using OperaLegacyLab.Web.Infrastructure;

namespace OperaLegacyLab.Web.Pages.Test;

public class JsDialogResultModel : LabPageModel
{
    private static readonly Dictionary<string, string> Labels = new()
    {
        ["alert"] = "JS alert()",
        ["confirm"] = "JS confirm()",
        ["prompt"] = "JS prompt()",
    };

    private readonly DeviceResultLog _deviceLog;

    public JsDialogResultModel(LabSessionStore store, DeviceResultLog deviceLog) : base(store) => _deviceLog = deviceLog;

    public string DialogType { get; private set; } = "unknown";
    public string Recorded { get; private set; } = "";

    // GET (not POST) on purpose: reached via location.href from inline
    // onclick handlers after a dialog is dismissed, not a form submit.
    public void OnGet(string? type, string? ok, string? value)
    {
        ResolveLab("Dialog test recorded");

        DialogType = type ?? "unknown";
        var isOk = ok == "1";
        var key = "js_dialog_" + DialogType;
        Lab.SelfReports[key] = isOk ? "yes - dismissed successfully" : "cancelled";
        string? note = null;
        if (value is not null)
        {
            // Decoded with Uri.UnescapeDataString: close enough to JS's escape()
            // for ordinary ASCII text, which covers the realistic test input here.
            note = "typed: " + Uri.UnescapeDataString(value);
            Lab.SelfReportNotes[key] = note;
        }
        Recorded = Lab.SelfReports[key];

        var label = Labels.TryGetValue(DialogType, out var l) ? l : "JS dialog (" + DialogType + ")";
        _deviceLog.Record(HttpContext, key, label, Recorded, note);
    }
}
