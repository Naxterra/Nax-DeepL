using System.Runtime.InteropServices;
using System.Windows;
using Application = System.Windows.Application;

namespace NaxTranslate;

/// <summary>Detects Ctrl+C+C (two Ctrl+C within 500 ms) via a low-level keyboard hook.</summary>
public sealed class HotkeyManager : IDisposable
{
    const int WH_KEYBOARD_LL = 13;
    const int WM_KEYDOWN     = 0x0100;
    const int WM_KEYUP       = 0x0101;
    const int VK_C           = 0x43;
    const int VK_CONTROL     = 0x11;

    delegate IntPtr LlKbProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")] static extern IntPtr SetWindowsHookEx(int id, LlKbProc proc, IntPtr mod, uint tid);
    [DllImport("user32.dll")] static extern bool   UnhookWindowsHookEx(IntPtr hhk);
    [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr hhk, int n, IntPtr w, IntPtr l);
    [DllImport("user32.dll")] static extern short  GetAsyncKeyState(int vk);
    [DllImport("kernel32.dll")] static extern IntPtr GetModuleHandle(string? name);

    [StructLayout(LayoutKind.Sequential)]
    struct KBDLLHOOKSTRUCT { public uint vkCode, scanCode, flags, time; public IntPtr extra; }

    readonly LlKbProc _proc;   // must hold reference to prevent GC
    readonly IntPtr   _hook;
    readonly CtrlCCDetector _detector = new();

    public event Action? CtrlCCPressed;

    public HotkeyManager()
    {
        _proc = HookCallback;
        using var proc = System.Diagnostics.Process.GetCurrentProcess();
        _hook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc,
                    GetModuleHandle(proc.MainModule!.ModuleName), 0);
    }

    IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == WM_KEYDOWN || wParam == WM_KEYUP)
            && Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam).vkCode == VK_C)
        {
            if (wParam == WM_KEYUP)
                _detector.OnCUp();
            else if (_detector.OnCDown((GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0, DateTime.UtcNow))
                // Small delay so the clipboard has the new content
                Task.Delay(80).ContinueWith(_ =>
                    Application.Current?.Dispatcher.Invoke(
                        () => CtrlCCPressed?.Invoke()));
        }
        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    public void Dispose() => UnhookWindowsHookEx(_hook);
}

/// <summary>
/// Decides when two presses of C with Ctrl held form a Ctrl+C+C. Windows repeats the key-down
/// message about 30 times a second while C is held, so a press only counts after C was released.
/// Without that, holding Ctrl+C fired the hotkey over and over and sent the clipboard to DeepL
/// each time.
/// </summary>
internal sealed class CtrlCCDetector
{
    static readonly TimeSpan Window = TimeSpan.FromMilliseconds(500);

    DateTime _lastPress = DateTime.MinValue;
    bool     _cHeld;

    /// <summary>Returns true when this press completes a Ctrl+C+C.</summary>
    public bool OnCDown(bool ctrlHeld, DateTime now)
    {
        if (_cHeld) return false;   // key repeat while C is still held
        _cHeld = true;
        if (ctrlHeld && now - _lastPress < Window)
        {
            _lastPress = DateTime.MinValue;
            return true;
        }
        _lastPress = ctrlHeld ? now : DateTime.MinValue;
        return false;
    }

    public void OnCUp() => _cHeld = false;
}
