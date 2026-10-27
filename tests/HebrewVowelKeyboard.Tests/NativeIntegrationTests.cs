using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Automation;
using HebrewVowelKeyboard.Input;
using HebrewVowelKeyboard.Models;

namespace HebrewVowelKeyboard.Tests;

// Tests only a newly launched native palette and the existing disposable editor.
// Every click is checked against the exact HWND of one of those two processes.
internal static class NativeIntegrationTests
{
    private sealed record EditorState(long Handle, string Text);

    internal static int Run(string executable)
    {
        var statePath = Path.Combine(AppContext.BaseDirectory, $"native-test-{Guid.NewGuid():N}.json");
        var previous = NativeMethods.GetForegroundWindow();
        GetCursorPos(out var pointer);
        Process? host = null, palette = null;
        KeyboardTarget? originalKeyboard = null;
        try
        {
            var start = new ProcessStartInfo(Environment.ProcessPath!, "--host") { UseShellExecute = false };
            start.Environment["HEBREW_TEST_STATE"] = statePath;
            host = Process.Start(start) ?? throw new Exception("Editor failed to start");
            Wait(() => File.Exists(statePath));
            EditorState State() => JsonSerializer.Deserialize<EditorState>(File.ReadAllText(statePath))!;
            var editor = (nint)State().Handle;
            NativeMethods.GetWindowThreadProcessId(editor, out var editorPid);
            Program.Equal((uint)host.Id, editorPid);
            SetForegroundWindow(editor);
            if (NativeMethods.GetForegroundWindow() != editor)
            {
                GetWindowRect(editor, out var rect);
                ClickPoint(editor, (rect.Left + rect.Right) / 2, (rect.Top + rect.Bottom) / 2);
            }
            Wait(() => NativeMethods.GetForegroundWindow() == editor);
            originalKeyboard = KeyboardLayouts.CurrentTarget() ?? throw new Exception("Editor has no input target");
            palette = Process.Start(new ProcessStartInfo(Path.GetFullPath(executable)) { UseShellExecute = false })
                ?? throw new Exception("Palette failed to start");
            nint paletteWindow = 0;
            Wait(() =>
            {
                EnumWindows((w, _) =>
                {
                    NativeMethods.GetWindowThreadProcessId(w, out var pid);
                    if (pid == palette.Id && IsWindowVisible(w)) { paletteWindow = w; return false; }
                    return true;
                }, 0);
                return paletteWindow != 0;
            });
            var ui = AutomationElement.FromHandle(paletteWindow);
            AutomationElement[] Buttons() => ui.FindAll(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button)).Cast<AutomationElement>().ToArray();
            AutomationElement Named(string name) => Buttons().First(b => b.Current.Name == name);
            void Click(AutomationElement button)
            {
                var bounds = button.Current.BoundingRectangle;
                ClickPoint(paletteWindow, (int)(bounds.Left + bounds.Width / 2), (int)(bounds.Top + bounds.Height / 2));
                Thread.Sleep(100);
                Program.Equal(editor, NativeMethods.GetForegroundWindow());
            }
            Wait(() => Buttons().Length >= 26);
            Program.Equal(editor, NativeMethods.GetForegroundWindow());
            Program.Equal(3L, (long)NativeMethods.SendMessage(paletteWindow, 0x0021, 0, 0));
            Wait(() => Named("English").Current.IsEnabled);
            Click(Named("English"));
            Wait(() => KeyboardLayouts.PrimaryLanguage(KeyboardLayouts.CurrentTarget()?.Layout ?? 0) == 9 && Named("עברית").Current.IsEnabled);
            Click(Named("Qamats"));
            Thread.Sleep(300);
            Program.Equal("ש", State().Text);
            Program.Equal(9, KeyboardLayouts.PrimaryLanguage(KeyboardLayouts.CurrentTarget()!.Layout));
            Console.WriteLine("PASS native English insertion guard and nonactivation");
            Click(Named("עברית"));
            Wait(() => KeyboardLayouts.IsHebrew(KeyboardLayouts.CurrentTarget()?.Layout ?? 0) && Named("English").Current.IsEnabled);
            Program.Equal("ש", State().Text);
            Console.WriteLine("PASS native language buttons preserve focus and never replay refused input");
            var expected = "ש";
            var count = 0;
            foreach (var isTeamim in new[] { false, true })
            {
                Click(Named(isTeamim ? "Te’amim" : "Niqqud"));
                var rows = isTeamim ? PaletteLayout.Teamim : PaletteLayout.Niqqud;
                var names = rows.SelectMany(r => r).Select(m => m.Name).ToHashSet();
                var buttons = Buttons().Where(b => names.Contains(b.Current.Name))
                    .OrderBy(b => Math.Round(b.Current.BoundingRectangle.Top))
                    .ThenByDescending(b => b.Current.BoundingRectangle.Left).ToArray();
                var marks = rows.SelectMany(r => r).ToArray();
                Program.Equal(marks.Length, buttons.Length);
                var frame = ui.Current.BoundingRectangle;
                for (int index = 0; index < marks.Length; index++)
                {
                    Program.Equal(marks[index].Name, buttons[index].Current.Name);
                    if (!frame.Contains(buttons[index].Current.BoundingRectangle)) throw new Exception("Clipped button");
                    Click(buttons[index]);
                    expected += marks[index].Text;
                    Wait(() => State().Text == expected);
                    count++;
                }
            }
            Program.Equal(54, count);
            Console.WriteLine("PASS all 54 native buttons follow RTL layout, fit and insert exact Unicode");
            SendKeys((0x10, false));
            try { Click(Named("Etnahta")); Thread.Sleep(250); Program.Equal(expected, State().Text); }
            finally { SendKeys((0x10, true)); }
            Console.WriteLine("PASS native modifier guard");
            Toggle();
            Wait(() => !IsWindowVisible(paletteWindow));
            Program.Equal(editor, NativeMethods.GetForegroundWindow());
            Toggle();
            Wait(() => IsWindowVisible(paletteWindow));
            Program.Equal(editor, NativeMethods.GetForegroundWindow());
            Console.WriteLine("PASS native show/hide shortcut preserves focus");
            NativeMethods.PostMessage(paletteWindow, 0x0010, 0, 0);
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
        finally
        {
            if (originalKeyboard is not null && KeyboardLayouts.SameTarget(originalKeyboard, KeyboardLayouts.CurrentTarget()))
            { KeyboardLayouts.RequestSelection(originalKeyboard, originalKeyboard.Layout); Thread.Sleep(150); }
            if (palette is not null && !palette.HasExited)
            {
                EnumWindows((w, _) =>
                {
                    NativeMethods.GetWindowThreadProcessId(w, out var pid);
                    if (pid == palette.Id) NativeMethods.PostMessage(w, 0x0010, 0, 0);
                    return true;
                }, 0);
                palette.WaitForExit(3000);
            }
            if (host is not null && !host.HasExited) { host.CloseMainWindow(); host.WaitForExit(3000); }
            palette?.Dispose(); host?.Dispose();
            SetCursorPos(pointer.X, pointer.Y);
            if (previous != 0) SetForegroundWindow(previous);
            foreach (var p in new[] { statePath, statePath + ".tmp" }) if (File.Exists(p)) File.Delete(p);
        }
    }
    private static void Wait(Func<bool> predicate)
    {
        var timer = Stopwatch.StartNew();
        while (timer.Elapsed.TotalSeconds < 8)
        {
            try { if (predicate()) return; } catch (IOException) { } catch (InvalidOperationException) { }
            Thread.Sleep(40);
        }
        throw new TimeoutException("Expected native palette or editor state was not observed");
    }
    private static void ClickPoint(nint window, int x, int y)
    {
        if (GetAncestor(WindowFromPoint(new Pointer { X = x, Y = y }), 2) != window)
            throw new Exception("Test window obscured; refusing to click another application");
        SetCursorPos(x, y);
        var inputs = new[] { Mouse(2), Mouse(4) };
        Program.Equal(2u, NativeMethods.SendInput(2, inputs, Marshal.SizeOf<NativeMethods.Input>()));
    }
    private static NativeMethods.Input Mouse(uint flags) => new()
    { Type = 0, Data = new NativeMethods.InputUnion { Mouse = new NativeMethods.MouseInput { Flags = flags } } };
    private static void Toggle() => SendKeys((0x11, false), (0x12, false), (0x48, false), (0x48, true), (0x12, true), (0x11, true));
    private static void SendKeys(params (ushort Key, bool Up)[] keys)
    {
        var inputs = keys.Select(k => new NativeMethods.Input
        { Type = 1, Data = new NativeMethods.InputUnion { Keyboard = new NativeMethods.KeyboardInput { VirtualKey = k.Key, Flags = k.Up ? 2u : 0u } } }).ToArray();
        Program.Equal((uint)inputs.Length, NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.Input>()));
    }
    [StructLayout(LayoutKind.Sequential)] private struct Pointer { internal int X, Y; }
    private delegate bool EnumWindowProc(nint window, nint param);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowProc proc, nint param);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint window, out NativeMethods.NativeRect rect);
    [DllImport("user32.dll")] private static extern nint WindowFromPoint(Pointer point);
    [DllImport("user32.dll")] private static extern nint GetAncestor(nint window, uint flags);
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out Pointer point);
}
