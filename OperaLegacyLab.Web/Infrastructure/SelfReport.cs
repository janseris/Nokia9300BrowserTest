namespace OperaLegacyLab.Web.Infrastructure;

public static class SelfReport
{
    /// <summary>
    /// A plain-HTML form (works with just &lt;form&gt; support, nothing
    /// fancier) used on every "does this look right?" test page. The server
    /// treats the answer as self-reported, not authoritative - unlike the
    /// cookie/forms/WML tests, which the server can verify directly.
    /// Deliberately has no `action` attribute - it self-posts back to
    /// whatever URL is currently in the address bar (including its "?lab="
    /// query string), which every page here handles with a plain OnPost().
    /// </summary>
    public static string Form(string labCode, string question)
    {
        return $"""
                <form method="post">
                <p><b>{Markup.Escape(question)}</b></p>
                <p>
                <input type="radio" name="result" value="yes" checked> Yes - looked correct<br>
                <input type="radio" name="result" value="partial"> Partly - some parts missing or wrong<br>
                <input type="radio" name="result" value="no"> No - blank, garbled, or broken
                </p>
                <p>Notes (optional):<br>
                <input type="text" name="note" size="30" maxlength="200">
                </p>
                <input type="hidden" name="lab" value="{Markup.Escape(labCode)}">
                <p><input type="submit" value="Submit result"></p>
                </form>
                """;
    }

    public static (string Result, string Note) ReadForm(IFormCollection form)
    {
        var result = form["result"].FirstOrDefault() ?? "no";
        var note = form["note"].FirstOrDefault() ?? "";
        return (result, note);
    }
}
