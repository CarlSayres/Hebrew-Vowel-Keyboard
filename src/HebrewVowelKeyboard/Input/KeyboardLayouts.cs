using System.Globalization;
using System.Runtime.InteropServices;

namespace HebrewVowelKeyboard.Input;

internal sealed record KeyboardTarget(nint Window, nint Focus, uint ThreadId, nint Layout);

internal static class KeyboardLayouts
{
    internal const int Hebrew = 0x0D;
    internal const int English = 0x09;
    internal const uint WmInputLanguageChangeRequest = 0x0050;

    internal static int PrimaryLanguage(nint layout) => (int)((long)layout & 0x3FF);
    internal static bool IsHebrew(nint layout) => PrimaryLanguage(layout) == Hebrew;

    internal static string LanguageName(nint layout)
    {
        if (layout == 0) return "Unknown";
        return PrimaryLanguage(layout) switch
        {
            Hebrew => "Hebrew",
            English => "English",
            _ => CultureName(layout)
        };
    }

    private static string CultureName(nint layout)
    {
        try { return CultureInfo.GetCultureInfo((int)((long)layout & 0xFFFF)).EnglishName; }
        catch (CultureNotFoundException) { return "Other"; }
    }

    internal static nint[] Installed()
    {
        var count = NativeMethods.GetKeyboardLayoutList(0, null);
        if (count <= 0) return [];
        var layouts = new nint[count];
        var copied = NativeMethods.GetKeyboardLayoutList(layouts.Length, layouts);
        return layouts.Take(Math.Max(0, copied)).ToArray();
    }

    // Query the editor's focused input thread, not the palette's keyboard.
    internal static KeyboardTarget? CurrentTarget()
    {
        var window = NativeMethods.GetForegroundWindow();
        if (window == 0) return null;
        var thread = NativeMethods.GetWindowThreadProcessId(window, out var process);
        if (thread == 0 || process == Environment.ProcessId) return null;
        var info = new NativeMethods.GuiThreadInfo { Size = (uint)Marshal.SizeOf<NativeMethods.GuiThreadInfo>() };
        if (!NativeMethods.GetGUIThreadInfo(thread, ref info) || info.Focus == 0) return null;
        var focusThread = NativeMethods.GetWindowThreadProcessId(info.Focus, out _);
        if (focusThread == 0 || NativeMethods.GetForegroundWindow() != window) return null;
        return new(window, info.Focus, focusThread, NativeMethods.GetKeyboardLayout(focusThread));
    }

    internal static bool SameTarget(KeyboardTarget target, KeyboardTarget? current) =>
        current is not null && target.Window == current.Window && target.Focus == current.Focus && target.ThreadId == current.ThreadId;

    // Only the explicit language buttons call this. Mark insertion never switches
    // layouts, installs a layout, or changes the Windows default input language.
    internal static bool RequestSelection(KeyboardTarget target, nint layout) =>
        Installed().Contains(layout) && SameTarget(target, CurrentTarget()) &&
        NativeMethods.PostMessage(target.Focus, WmInputLanguageChangeRequest, 0, layout);
}
