using Microsoft.Win32;

namespace NaxTranslate;

public static class StartupHelper
{
    const string RunKey  = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string AppName = "Nax-Translate";

    // Older versions registered themselves under these names: "Nax-DeepL" up to 1.1.x and
    // "DeepL-Translator" before that.
    static readonly string[] LegacyNames = ["Nax-DeepL", "DeepL-Translator"];

    /// <summary>
    /// Added to the "Start with Windows" entry, so the app can tell a start at sign-in from a start
    /// by hand. Only a start by hand shows a short note that the app now runs in the tray.
    /// </summary>
    public const string AutostartSwitch = "--autostart";

    static string ExePath => Environment.ProcessPath
                             ?? System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;

    public static bool IsEnabled() => IsEnabled(Registry.CurrentUser, RunKey);

    public static void SetEnabled(bool enabled) => SetEnabled(enabled, ExePath, Registry.CurrentUser, RunKey);

    /// <summary>
    /// Replaces an entry of an older version, which points at that version's program file, with
    /// one for this app.
    /// </summary>
    public static void MoveLegacyEntry() => MoveLegacyEntry(ExePath, Registry.CurrentUser, RunKey);

    internal static bool IsEnabled(RegistryKey root, string runKeyPath)
    {
        using var key = root.OpenSubKey(runKeyPath);
        return key?.GetValue(AppName) != null;
    }

    internal static void SetEnabled(bool enabled, string exe, RegistryKey root, string runKeyPath)
    {
        using var key = enabled ? root.CreateSubKey(runKeyPath) : root.OpenSubKey(runKeyPath, writable: true);
        if (key == null) return;
        if (enabled)
            key.SetValue(AppName, $"\"{exe}\" {AutostartSwitch}");
        else
            key.DeleteValue(AppName, throwOnMissingValue: false);
        foreach (var legacy in LegacyNames)
            key.DeleteValue(legacy, throwOnMissingValue: false);
    }

    internal static void MoveLegacyEntry(string exe, RegistryKey root, string runKeyPath)
    {
        bool hasLegacy;
        using (var key = root.OpenSubKey(runKeyPath))
            hasLegacy = LegacyNames.Any(n => key?.GetValue(n) != null);
        if (hasLegacy) SetEnabled(true, exe, root, runKeyPath);
    }
}
