using Microsoft.Win32;

namespace NaxTranslate.Tests;

/// <summary>
/// Runs the "Start with Windows" helper against a throw-away key under HKCU, never against the
/// real Run key. It shares its parent test key with SchemeHandlerTests, so both run in one
/// collection: run in parallel, one class could delete the parent key while the other writes below it.
/// </summary>
[Collection("RegistryTests")]
public sealed class StartupTests : IDisposable
{
    readonly string _base = $@"Software\NaxTranslate.Tests\{Guid.NewGuid():N}";
    string Run => $@"{_base}\Run";

    public void Dispose()
    {
        Registry.CurrentUser.DeleteSubKeyTree(_base, throwOnMissingSubKey: false);
        using var parent = Registry.CurrentUser.OpenSubKey(@"Software\NaxTranslate.Tests");
        if (parent is { SubKeyCount: 0, ValueCount: 0 })
            Registry.CurrentUser.DeleteSubKey(@"Software\NaxTranslate.Tests", throwOnMissingSubKey: false);
    }

    [Fact]
    public void start_with_windows_switched_on_registers_quoted_app_path()
    {
        StartupHelper.SetEnabled(true, @"C:\Apps\Nax Translate\Nax-Translate.exe", Registry.CurrentUser, Run);

        using var key = Registry.CurrentUser.OpenSubKey(Run);
        Assert.Equal("\"C:\\Apps\\Nax Translate\\Nax-Translate.exe\"", key?.GetValue("Nax-Translate"));
        Assert.True(StartupHelper.IsEnabled(Registry.CurrentUser, Run));
    }

    [Fact]
    public void start_with_windows_switched_off_removes_entry_and_keeps_other_programs()
    {
        using (var key = Registry.CurrentUser.CreateSubKey(Run))
            key.SetValue("OtherProgram", "\"C:\\Other\\other.exe\"");
        StartupHelper.SetEnabled(true, @"C:\Apps\app.exe", Registry.CurrentUser, Run);

        StartupHelper.SetEnabled(false, @"C:\Apps\app.exe", Registry.CurrentUser, Run);

        using var run = Registry.CurrentUser.OpenSubKey(Run);
        Assert.Null(run?.GetValue("Nax-Translate"));
        Assert.Equal("\"C:\\Other\\other.exe\"", run?.GetValue("OtherProgram"));
        Assert.False(StartupHelper.IsEnabled(Registry.CurrentUser, Run));
    }

    [Fact]
    public void start_with_windows_switched_off_also_removes_entry_under_old_app_name()
    {
        using (var key = Registry.CurrentUser.CreateSubKey(Run))
            key.SetValue("DeepL-Translator", "\"C:\\Old\\Better DeepL Translator.exe\"");

        StartupHelper.SetEnabled(false, @"C:\Apps\Nax-Translate.exe", Registry.CurrentUser, Run);

        using var run = Registry.CurrentUser.OpenSubKey(Run);
        Assert.Null(run?.GetValue("DeepL-Translator"));
        Assert.Null(run?.GetValue("Nax-Translate"));
    }

    [Fact]
    public void old_start_with_windows_entry_replaced_by_one_for_renamed_app()
    {
        using (var key = Registry.CurrentUser.CreateSubKey(Run))
            key.SetValue("DeepL-Translator", "\"C:\\Old\\Better DeepL Translator.exe\"");

        StartupHelper.MoveLegacyEntry(@"C:\Apps\Nax-Translate.exe", Registry.CurrentUser, Run);

        using var run = Registry.CurrentUser.OpenSubKey(Run);
        Assert.Null(run?.GetValue("DeepL-Translator"));
        Assert.Equal("\"C:\\Apps\\Nax-Translate.exe\"", run?.GetValue("Nax-Translate"));
    }

    [Fact]
    public void start_with_windows_entry_of_nax_deepl_replaced_by_one_for_nax_translate()
    {
        using (var key = Registry.CurrentUser.CreateSubKey(Run))
            key.SetValue("Nax-DeepL", "\"C:\\Old\\Nax-DeepL.exe\"");

        StartupHelper.MoveLegacyEntry(@"C:\Apps\Nax-Translate.exe", Registry.CurrentUser, Run);

        using var run = Registry.CurrentUser.OpenSubKey(Run);
        Assert.Null(run?.GetValue("Nax-DeepL"));
        Assert.Equal("\"C:\\Apps\\Nax-Translate.exe\"", run?.GetValue("Nax-Translate"));
    }

    [Fact]
    public void start_with_windows_switched_off_also_removes_entry_of_nax_deepl()
    {
        using (var key = Registry.CurrentUser.CreateSubKey(Run))
            key.SetValue("Nax-DeepL", "\"C:\\Old\\Nax-DeepL.exe\"");

        StartupHelper.SetEnabled(false, @"C:\Apps\Nax-Translate.exe", Registry.CurrentUser, Run);

        using var run = Registry.CurrentUser.OpenSubKey(Run);
        Assert.Null(run?.GetValue("Nax-DeepL"));
        Assert.False(StartupHelper.IsEnabled(Registry.CurrentUser, Run));
    }

    [Fact]
    public void start_without_old_entry_does_not_switch_on_start_with_windows()
    {
        StartupHelper.MoveLegacyEntry(@"C:\Apps\Nax-Translate.exe", Registry.CurrentUser, Run);

        Assert.False(StartupHelper.IsEnabled(Registry.CurrentUser, Run));
    }

    [Fact]
    public void start_with_windows_switched_off_when_never_on_changes_nothing()
    {
        StartupHelper.SetEnabled(false, @"C:\Apps\app.exe", Registry.CurrentUser, Run);

        using var key = Registry.CurrentUser.OpenSubKey(Run);
        Assert.Null(key);
    }
}
