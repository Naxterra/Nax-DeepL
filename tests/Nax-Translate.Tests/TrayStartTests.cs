using System.IO;

namespace NaxTranslate.Tests;

/// <summary>Covers when the app starts hidden in the tray and when it shows its window.</summary>
public sealed class TrayStartTests : IDisposable
{
    readonly string _dir = Path.Combine(Path.GetTempPath(), "NaxTranslate.Tests", Guid.NewGuid().ToString("N"));
    string ConfigPath => Path.Combine(_dir, "config.json");

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void app_starts_hidden_in_tray_when_option_is_on()
    {
        Assert.True(App.ShouldStartHidden(startInTray: true, []));
    }

    [Fact]
    public void app_shows_window_at_start_when_option_is_off()
    {
        Assert.False(App.ShouldStartHidden(startInTray: false, []));
    }

    [Fact]
    public void launch_after_setup_with_show_switch_opens_window_despite_tray_option()
    {
        Assert.False(App.ShouldStartHidden(startInTray: true, ["--show"]));
        Assert.False(App.ShouldStartHidden(startInTray: true, ["--SHOW"]));
    }

    [Fact]
    public void hidden_start_by_hand_shows_note_that_app_runs_in_tray()
    {
        Assert.True(App.ShouldNotifyTrayStart(startsHidden: true, []));
    }

    [Fact]
    public void hidden_start_at_windows_sign_in_stays_quiet()
    {
        Assert.False(App.ShouldNotifyTrayStart(startsHidden: true, [StartupHelper.AutostartSwitch]));
    }

    [Fact]
    public void start_with_window_shown_needs_no_tray_note()
    {
        Assert.False(App.ShouldNotifyTrayStart(startsHidden: false, []));
    }

    [Fact]
    public void start_in_tray_and_minimize_to_tray_are_on_by_default_and_kept_when_switched_off()
    {
        var cfg = Config.Load(ConfigPath);
        Assert.True(cfg.StartInTray);
        Assert.True(cfg.MinimizeToTray);
        cfg.StartInTray = false;
        cfg.MinimizeToTray = false;
        cfg.Save();

        var reloaded = Config.Load(ConfigPath);
        Assert.False(reloaded.StartInTray);
        Assert.False(reloaded.MinimizeToTray);
    }
}
