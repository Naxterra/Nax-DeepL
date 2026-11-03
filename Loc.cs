namespace NaxDeepL;

public static class Loc
{
    public static string Current { get; private set; } = "de";
    public static event Action? Changed;

    public static void SetLanguage(string lang)
    {
        if (Current == lang) return;
        Current = lang;
        Changed?.Invoke();
    }

    static bool En => Current == "en";

    public static string AppTitle        => "Nax-DeepL";
    public static string AboutBlurb      => En ? "A fast DeepL Pro translator for Windows that keeps working when the network changes."
                                               : "Ein schneller DeepL-Pro-Übersetzer für Windows, der auch nach einem Netzwerkwechsel weiterarbeitet.";
    public static string AboutAuthor     => En ? "Author"                                  : "Autor";
    public static string BtnConnect      => En ? "Connect"                                 : "Verbindung";
    public static string MenuLogin        => En ? "Login"                                   : "Anmelden";
    public static string MenuSwitch      => En ? "Switch Account"                          : "Konto wechseln";
    public static string MenuLogout      => En ? "Logout"                                  : "Abmelden";
    public static string BtnClear        => En ? "Clear"                                   : "Löschen";
    public static string BtnCopy         => En ? "Copy"                                    : "Kopieren";
    public static string StatusReady     => En ? "Ready"                                   : "Bereit";
    public static string StatusChecking  => En ? "Checking login…"                         : "Anmeldung wird geprüft…";
    public static string StatusBrowser   => En ? "Log in in the browser, then click \"Open app\". If nothing happens, copy the page address from the address bar."
                                               : "Im Browser anmelden, dann „App öffnen“ klicken. Passiert nichts, die Adresse aus der Adressleiste kopieren.";
    public static string StatusLoggedIn  => En ? "Logged in."                              : "Angemeldet.";
    public static string StatusLoggedOut => En ? "Logged out."                             : "Abgemeldet.";
    public static string StatusCancelled => En ? "Login cancelled or failed."              : "Anmeldung abgebrochen oder fehlgeschlagen.";
    public static string StatusNotAuth   => En ? "Not logged in. Click ⚙ Connect."         : "Nicht angemeldet. Bitte ⚙ Verbindung klicken.";
    public static string StatusCopied    => En ? "Copied."                                 : "Kopiert.";
    public static string StatusClipboardBusy => En ? "The clipboard is in use by another program. Please try again."
                                                   : "Die Zwischenablage wird von einem anderen Programm benutzt. Bitte noch einmal versuchen.";
    public static string NoTranslation   => En ? "No translation received."                : "Keine Übersetzung erhalten.";
    public static string StatusClaudeBusy => En ? "Claude is busy right now (too many requests). Please try again in a moment."
                                                : "Claude ist gerade ausgelastet (zu viele Anfragen). Bitte gleich noch einmal versuchen.";
    public static string StatusLimitReached => En ? "DeepL is not accepting more translations right now because a usage limit was reached. Please try again later."
                                                  : "DeepL nimmt gerade keine weiteren Übersetzungen an, weil ein Nutzungslimit erreicht ist. Bitte später noch einmal versuchen.";
    public static string StatusClaudeLimit(string detail) =>
        En ? $"Claude is not accepting more requests right now because your subscription's usage limit was reached ({detail})."
           : $"Claude nimmt gerade keine weiteren Anfragen an, weil das Nutzungslimit deines Abos erreicht ist ({detail}).";
    public static string StatusClaudeNotFound => En ? "Claude Code is not installed. Install it with \"winget install Anthropic.ClaudeCode\" and sign in once by running \"claude\"."
                                                    : "Claude Code ist nicht installiert. Mit „winget install Anthropic.ClaudeCode“ installieren und einmal „claude“ starten, um dich anzumelden.";
    public static string StatusTooLong(int length) =>
        En ? $"The text has {length} characters and was not sent. Please shorten it to {DeepLApi.MaxChars}."
           : $"Der Text hat {length} Zeichen und wurde nicht verschickt. Bitte auf {DeepLApi.MaxChars} kürzen.";
    public static string StatusTranslating => En ? "Translating…"                          : "Übersetze…";
    public static string StatusError     => En ? "Error: "                                 : "Fehler: ";
    public static string StatusOffline   => En ? "No connection to DeepL. Retrying automatically…"
                                               : "Keine Verbindung zu DeepL. Neuer Versuch läuft automatisch…";

    public static string MenuStartup      => En ? "Start with Windows"                      : "Mit Windows starten";
    public static string MenuAbout        => En ? "About"                                   : "Über";

    public static string StatusTranslated(string? lang, string engine, TimeSpan took)
    {
        var secs = took.TotalSeconds.ToString("0.0", En ? System.Globalization.CultureInfo.InvariantCulture
                                                        : System.Globalization.CultureInfo.GetCultureInfo("de-DE"));
        return lang != null
            ? (En ? $"Translated by {engine} in {secs} s (detected: {lang})" : $"Übersetzt mit {engine} in {secs} s (erkannt: {lang})")
            : (En ? $"Translated by {engine} in {secs} s" : $"Übersetzt mit {engine} in {secs} s");
    }
}
