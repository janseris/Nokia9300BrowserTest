using OperaLegacyLab.Web.Infrastructure;

namespace OperaLegacyLab.Web.Pages;

public class UaProfModel : LabPageModel
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly DeviceResultLog _deviceLog;

    public UaProfModel(LabSessionStore store, IHttpClientFactory httpFactory, DeviceResultLog deviceLog) : base(store)
    {
        _httpFactory = httpFactory;
        _deviceLog = deviceLog;
    }

    public async Task OnGetAsync()
    {
        ResolveLab("WAP UAProf");

        if (string.IsNullOrEmpty(Lab.WapProfileUrl))
        {
            _deviceLog.Record(HttpContext, "uaprof", "WAP UAProf", "not advertised by this browser");
            return;
        }

        if (Lab.WapProfileFetchAttempted)
        {
            _deviceLog.Record(HttpContext, "uaprof", "WAP UAProf", Lab.WapProfileUrl, Lab.WapProfileFetchStatus);
            return;
        }

        Lab.WapProfileFetchAttempted = true;
        try
        {
            var client = _httpFactory.CreateClient("uaprof");
            using var resp = await client.GetAsync(Lab.WapProfileUrl);
            var text = await resp.Content.ReadAsStringAsync();
            Lab.WapProfileFetchStatus = $"HTTP {(int)resp.StatusCode} {resp.StatusCode}";
            Lab.WapProfileSnippet = text.Length > 4000 ? text[..4000] + "\n... (truncated)" : text;
        }
        catch (Exception ex)
        {
            Lab.WapProfileFetchStatus = "Failed: " + ex.Message;
            Lab.WapProfileSnippet = null;
        }

        _deviceLog.Record(HttpContext, "uaprof", "WAP UAProf", Lab.WapProfileUrl, Lab.WapProfileFetchStatus);
    }
}
