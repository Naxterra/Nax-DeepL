using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Windows;
using Application = System.Windows.Application;
using Forms = System.Windows.Forms;

namespace NaxTranslate;

public partial class App : Application
{
    const string PipeName = "Nax-Translate-Auth";

    /// <summary>
    /// What a second start of the app sends to the running one, so that it shows its window. With
    /// the window hidden in the tray, starting the app again would otherwise seem to do nothing.
    /// </summary>
    internal const string ShowCommand = "nax-translate:show";

    /// <summary>The switch that opens the window even when the app normally starts in the tray.</summary>
    internal const string ShowSwitch = "--show";

    /// <summary>Whether this start keeps the window hidden and shows only the tray icon.</summary>
    internal static bool ShouldStartHidden(bool startInTray, string[] args) =>
        startInTray && !args.Any(a => string.Equals(a, ShowSwitch, StringComparison.OrdinalIgnoreCase));

    static Mutex?              _mutex;
    internal Forms.NotifyIcon? TrayIcon;
    internal HotkeyManager?   Hotkey;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // If launched with a deepl:// callback URL, forward it to the running instance
        if (e.Args.Length > 0 && e.Args[0].StartsWith("deepl://", StringComparison.OrdinalIgnoreCase))
        {
            SendToRunningApp(e.Args[0]);
            Shutdown();
            return;
        }

        // The uninstaller runs the app once with this switch, so that nothing the app wrote to
        // the registry outlives it: the "Start with Windows" entry and a login's deepl:// handler.
        if (e.Args.Length > 0 && e.Args[0] == "--uninstall-cleanup")
        {
            AuthManager.RestoreLeftoverSchemeHandler();
            StartupHelper.SetEnabled(false);
            Shutdown();
            return;
        }

        // Single instance guard. A second start asks the running app to show its window and ends.
        _mutex = new Mutex(true, "Nax-Translate-7F3A", out bool first);
        if (!first)
        {
            SendToRunningApp(ShowCommand);
            Shutdown();
            return;
        }

        // Undo the deepl:// handler change of a login that a crash cut short, then start
        // replacing dead connections whenever Windows reports a network change.
        AuthManager.RestoreLeftoverSchemeHandler();
        Net.WatchNetworkChanges();

        // A "Start with Windows" entry from before the rename points at the old program file.
        try { StartupHelper.MoveLegacyEntry(); } catch { }

        // Listen for OAuth callbacks from second instances
        _ = Task.Run(ListenForCallbacks);

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Tray icon
        TrayIcon = new Forms.NotifyIcon
        {
            Text    = Loc.AppTitle,
            Icon    = LoadIcon(),
            Visible = true,
        };
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(Loc.TrayOpen, null, (_, _) => ShowMain());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(Loc.TrayExit, null, (_, _) => ExitApp());
        TrayIcon.ContextMenuStrip = menu;
        // One click with the left button opens the window, as with other tray apps on Windows 11.
        TrayIcon.MouseClick += (_, me) => { if (me.Button == Forms.MouseButtons.Left) ShowMain(); };

        // Global Ctrl+C+C hotkey
        Hotkey = new HotkeyManager();
        Hotkey.CtrlCCPressed += () => ((MainWindow?)MainWindow)?.OnCtrlCC();

        // The app starts in the tray unless that option is off or it was started with --show,
        // which the installer uses so that "Launch" after setup opens the window.
        var win = new MainWindow();
        if (ShouldStartHidden(win.StartsInTray, e.Args)) win.StartHidden();
        else                                            win.Show();
    }

    static void SendToRunningApp(string message)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(2000);
            using var w = new StreamWriter(client) { AutoFlush = true };
            w.Write(message);
        }
        catch { }
    }

    async Task ListenForCallbacks()
    {
        while (true)
        {
            try
            {
                using var server = new NamedPipeServerStream(PipeName, PipeDirection.In,
                    maxNumberOfServerInstances: 1, transmissionMode: PipeTransmissionMode.Byte,
                    options: PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync();
                using var reader = new StreamReader(server);
                var message = (await reader.ReadToEndAsync()).Trim();
                if (message == ShowCommand)
                    Dispatcher.Invoke(ShowMain);
                else if (message.Length > 0)
                    Dispatcher.Invoke(() => AuthManager.HandleCallback(message));
            }
            catch { await Task.Delay(500); }
        }
    }

    internal void ShowMain()
    {
        if (MainWindow is MainWindow w)
        {
            w.Show();
            w.WindowState = WindowState.Normal;
            w.Activate();
        }
    }

    void ExitApp()
    {
        // A Claude run would keep going after the app is gone and use up quota.
        (MainWindow as MainWindow)?.StopBackgroundWork();
        (MainWindow as MainWindow)?.SaveSettings();
        TrayIcon?.Dispose();
        Hotkey?.Dispose();
        _mutex?.ReleaseMutex();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Also covers ending the app through Windows sign-out or shutdown, which skips ExitApp.
        (MainWindow as MainWindow)?.StopBackgroundWork();
        TrayIcon?.Dispose();
        Hotkey?.Dispose();
        base.OnExit(e);
    }

    static System.Drawing.Icon LoadIcon()
    {
        var uri  = new Uri("pack://application:,,,/icon.ico");
        var info = Application.GetResourceStream(uri);
        // The tray shows small icons, so pick the size Windows uses for them at the current display
        // scale (16, 20 or 24 pixels) instead of scaling down a large one.
        return info is not null
            ? new System.Drawing.Icon(info.Stream, Forms.SystemInformation.SmallIconSize)
            : System.Drawing.SystemIcons.Application;
    }
}

