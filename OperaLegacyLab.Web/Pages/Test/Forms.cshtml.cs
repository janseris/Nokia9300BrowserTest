using OperaLegacyLab.Web.Infrastructure;

namespace OperaLegacyLab.Web.Pages.Test;

public class FormsModel : LabPageModel
{
    private readonly DeviceResultLog _deviceLog;

    public FormsModel(LabSessionStore store, DeviceResultLog deviceLog) : base(store) => _deviceLog = deviceLog;

    public bool Submitted { get; private set; }
    public List<(string Key, string Value)> PostedValues { get; private set; } = new();
    public bool CheckboxKeyPresent { get; private set; }

    public void OnGet() => ResolveLab("Form controls test");

    public void OnPost()
    {
        ResolveLab("Form submission received");

        Submitted = true;
        Lab.FormPosted = true;
        Lab.FormPostedValues.Clear();
        foreach (var key in Request.Form.Keys)
        {
            if (key is "lab") continue;
            var value = string.Join(" | ", Request.Form[key].ToArray().Select(v => v ?? ""));
            Lab.FormPostedValues[key] = value;
        }
        PostedValues = Lab.FormPostedValues.Select(kv => (kv.Key, kv.Value)).ToList();
        CheckboxKeyPresent = Request.Form.ContainsKey("checkbox_field");

        var summary = string.Join(", ", PostedValues.Select(kv => $"{kv.Key}={kv.Value}"));
        _deviceLog.Record(HttpContext, "forms", "Form POST", "submitted", summary);
    }
}
