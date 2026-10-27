using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using HebrewVowelKeyboard.Input;
using HebrewVowelKeyboard.Models;

namespace HebrewVowelKeyboard.Tests;

// An interactive test fixture, not an automation layer in the shipped app.
// Only the disposable editor started here is used as an input target.
internal static class IntegrationTests
{
    private sealed record EditorState(long Handle, string Text);
    private static string StatePath => Environment.GetEnvironmentVariable("HEBREW_TEST_STATE")
        ?? throw new InvalidOperationException("Missing test state path.");

    internal static int RunHost()
    {
        var application = new Application();
        var editor = new TextBox
        {
            Text = "ש", FontSize = 48, FontFamily = new FontFamily("Times New Roman"),
            FlowDirection = FlowDirection.RightToLeft, Margin = new Thickness(24),
            AcceptsReturn = true
        };
        var window = new Window
        {
            Title = "Hebrew Marks — disposable integration editor",
            Width = 550, Height = 250, Left = 40, Top = 100, Topmost = true, Content = editor
        };
        void SaveState()
        {
            var state = new EditorState((long)new WindowInteropHelper(window).Handle, editor.Text);
            File.WriteAllText(StatePath + ".tmp", JsonSerializer.Serialize(state));
            File.Move(StatePath + ".tmp", StatePath, true);
        }
        editor.TextChanged += (_, _) => SaveState();
        window.Loaded += (_, _) => { editor.Focus(); editor.CaretIndex = editor.Text.Length; SaveState(); };
        application.Run(window);
        return 0;
    }

    internal static int Run()
    {
        var statePath = Path.Combine(AppContext.BaseDirectory, $"hebrew-marks-test-{Guid.NewGuid():N}.json");
        var previousForeground = NativeMethods.GetForegroundWindow();
        GetCursorPos(out var previousPointer);
        var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var palette = new MainWindow();
        Process? host = null;
        KeyboardTarget? originalKeyboard = null;
        var exitCode = 1;
        palette.Loaded += async (_, _) =>
        {
            try
            {
                var start = new ProcessStartInfo(Environment.ProcessPath!, "--host") { UseShellExecute = false };
                start.Environment["HEBREW_TEST_STATE"] = statePath;
                host = Process.Start(start) ?? throw new InvalidOperationException("Editor did not start.");
                await WaitUntil(() => File.Exists(statePath));
                var target = (nint)ReadState(statePath).Handle;
                if (NativeMethods.GetForegroundWindow() != target)
                {
                    SetForegroundWindow(target);
                    if (NativeMethods.GetForegroundWindow() != target)
                    {
                        // Windows may deny programmatic activation when the runner
                        // was launched in the background. Click only our own editor.
                        GetWindowRect(target, out var bounds);
                        var point = new Pointer { X = (bounds.Left + bounds.Right) / 2, Y = (bounds.Top + bounds.Bottom) / 2 };
                        if (GetAncestor(WindowFromPoint(point), 2) != target)
                            throw new InvalidOperationException("Test editor is obscured; refusing to click another window.");
                        SetCursorPos(point.X, point.Y);
                        var clicks = new[] { MouseEvent(0x0002), MouseEvent(0x0004) };
                        Program.Equal(2u, NativeMethods.SendInput(2, clicks, Marshal.SizeOf<NativeMethods.Input>()));
                    }
                }
                await WaitUntil(() => NativeMethods.GetForegroundWindow() == target);

                originalKeyboard = KeyboardLayouts.CurrentTarget() ?? throw new InvalidOperationException("Cannot inspect the disposable editor keyboard.");
                var installed = KeyboardLayouts.Installed();
                if (!installed.Any(layout => KeyboardLayouts.PrimaryLanguage(layout) == KeyboardLayouts.English) ||
                    !installed.Any(KeyboardLayouts.IsHebrew))
                    throw new InvalidOperationException("This integration test requires installed English and Hebrew keyboards.");

                // Use the actual header buttons; selection is manual and must never
                // insert a previously refused mark or take focus from the editor.
                await WaitUntil(() => ((Button)palette.FindName("EnglishKeyboardButton")).IsEnabled);
                Click((Button)palette.FindName("EnglishKeyboardButton"));
                await WaitUntil(() => KeyboardLayouts.PrimaryLanguage(KeyboardLayouts.CurrentTarget()?.Layout ?? 0) == KeyboardLayouts.English &&
                    ((TextBlock)palette.FindName("KeyboardStatus")).Text == "Keyboard: English");
                await WaitUntil(() => ((Button)palette.FindName("HebrewKeyboardButton")).IsEnabled);
                Click(Descendants<Button>(palette).Single(control => control.Tag is HebrewMark mark && mark.Id == "qamats"));
                await WaitUntil(() => ((TextBlock)palette.FindName("ShortcutText")).Text == "Select Hebrew keyboard");
                await Task.Delay(150);
                Program.Equal("ש", ReadState(statePath).Text);
                Program.Equal(KeyboardLayouts.English, KeyboardLayouts.PrimaryLanguage(KeyboardLayouts.CurrentTarget()!.Layout));
                Program.Equal(target, NativeMethods.GetForegroundWindow());
                Console.WriteLine("PASS English keyboard is displayed; mark clicks warn without inserting or switching language");

                Click((Button)palette.FindName("HebrewKeyboardButton"));
                await WaitUntil(() => KeyboardLayouts.IsHebrew(KeyboardLayouts.CurrentTarget()?.Layout ?? 0) &&
                    ((TextBlock)palette.FindName("KeyboardStatus")).Text == "Keyboard: Hebrew" &&
                    ((Button)palette.FindName("EnglishKeyboardButton")).IsEnabled);
                Program.Equal("ש", ReadState(statePath).Text);
                Program.Equal(target, NativeMethods.GetForegroundWindow());
                Console.WriteLine("PASS Header keyboard buttons switch only on request and retain editor focus without replaying refused input");

                var paletteHandle = new WindowInteropHelper(palette).Handle;
                Program.Equal(3L, (long)NativeMethods.SendMessage(paletteHandle, NativeMethods.WmMouseActivate, 0, 0));
                Console.WriteLine("PASS Palette refuses mouse activation without discarding clicks");

                var expected = "ש";
                var clicked = 0;
                foreach (var group in new[] { "Niqqud", "Te'amim" })
                {
                    Click((Button)palette.FindName(group == "Niqqud" ? "NiqqudTab" : "TeamimTab"));
                    await Task.Delay(200);
                    Program.Equal(target, NativeMethods.GetForegroundWindow());
                    var rows = group == "Niqqud" ? PaletteLayout.Niqqud : PaletteLayout.Teamim;
                    var renderedButtons = Descendants<Button>(palette).Where(control => control.Tag is HebrewMark).ToArray();
                    Program.Equal(rows.Sum(row => row.Count), renderedButtons.Length);
                    var rowIndex = 0;
                    double previousRowTop = -1;
                    foreach (var row in rows)
                    {
                        double previousLeft = double.PositiveInfinity;
                        double? rowTop = null;
                        foreach (var mark in row)
                        {
                            var button = renderedButtons[rowIndex++];
                            Program.Equal(mark.Id, ((HebrewMark)button.Tag).Id);
                            var bounds = button.TransformToAncestor(palette).TransformBounds(new Rect(button.RenderSize));
                            if (!new Rect(0, 0, palette.ActualWidth, palette.ActualHeight).Contains(bounds))
                                throw new InvalidOperationException($"Button is clipped: {mark.Name}");
                            if (bounds.Left >= previousLeft || (rowTop.HasValue && Math.Abs(rowTop.Value - bounds.Top) > 1))
                                throw new InvalidOperationException($"Row is not ordered right to left: {mark.Name}");
                            previousLeft = bounds.Left;
                            rowTop = bounds.Top;
                            Click(button);
                            clicked++;
                            expected += mark.Text;
                            await WaitUntil(() => ReadState(statePath).Text == expected);
                            Program.Equal(target, NativeMethods.GetForegroundWindow());
                        }
                        if (rowTop!.Value <= previousRowTop) throw new InvalidOperationException("Rows overlap.");
                        previousRowTop = rowTop.Value;
                    }
                }
                Program.Equal("ש" + string.Concat(PaletteLayout.Niqqud.Concat(PaletteLayout.Teamim).SelectMany(row => row).Select(mark => mark.Text)), ReadState(statePath).Text);
                Console.WriteLine($"PASS All {clicked} buttons follow their right-to-left rows, fit without scrolling and insert exact Unicode while retaining focus");

                Click((Button)palette.FindName("NiqqudTab"));
                await Task.Delay(200);
                Program.Equal(target, NativeMethods.GetForegroundWindow());
                Click(Descendants<Button>(palette).Single(control => control.Tag is HebrewMark value && value.Id == "patah"));
                expected += "\u05B7";
                await WaitUntil(() => ReadState(statePath).Text == expected);
                Program.Equal(target, NativeMethods.GetForegroundWindow());
                Console.WriteLine("PASS Switching panels preserves the editor target");

                // A held modifier must cause a refusal, rather than an accidental editor shortcut.
                SendKeys((0x11, false));
                var blocked = new UnicodeInputSender().Insert(MarkCatalog.Find("qamats"));
                SendKeys((0x11, true));
                Program.Equal(false, blocked.Success);
                Program.Equal(expected, ReadState(statePath).Text);
                Console.WriteLine("PASS Held modifiers block insertion without changing text");

                if (palette.IsHotkeyRegistered)
                {
                    ToggleHotkey();
                    await WaitUntil(() => !palette.IsVisible);
                    ToggleHotkey();
                    await WaitUntil(() => palette.IsVisible);
                    Program.Equal(target, NativeMethods.GetForegroundWindow());
                    Console.WriteLine("PASS Show/hide shortcut preserves editor focus");
                }
                else Console.WriteLine("SKIP Shortcut unavailable (registration failed)");
                exitCode = 0;
            }
            catch (Exception exception) { Console.Error.WriteLine($"FAIL {exception}"); }
            finally
            {
                SendKeys((0x11, true), (0x12, true));
                if (originalKeyboard is not null && KeyboardLayouts.SameTarget(originalKeyboard, KeyboardLayouts.CurrentTarget()))
                {
                    KeyboardLayouts.RequestSelection(originalKeyboard, originalKeyboard.Layout);
                    await Task.Delay(100);
                }
                palette.Close();
                if (host is { HasExited: false })
                {
                    host.CloseMainWindow();
                    if (!host.WaitForExit(2000)) host.Kill();
                }
                host?.Dispose();
                SetCursorPos(previousPointer.X, previousPointer.Y);
                if (previousForeground != 0) SetForegroundWindow(previousForeground);
                foreach (var path in new[] { statePath, statePath + ".tmp" })
                    if (File.Exists(path)) File.Delete(path);
                application.Shutdown(exitCode);
            }
        };
        application.Run(palette);
        return exitCode;
    }

    private static EditorState ReadState(string path) => JsonSerializer.Deserialize<EditorState>(File.ReadAllText(path))!;

    private static async Task WaitUntil(Func<bool> predicate)
    {
        var timer = Stopwatch.StartNew();
        while (timer.Elapsed < TimeSpan.FromSeconds(6))
        {
            try { if (predicate()) return; } catch (IOException) { }
            await Task.Delay(40);
        }
        throw new TimeoutException("The expected editor or palette state was not observed.");
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match) yield return match;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }

    private static void Click(FrameworkElement element)
    {
        var position = element.PointToScreen(new Point(element.ActualWidth / 2, element.ActualHeight / 2));
        SetCursorPos((int)position.X, (int)position.Y);
        var events = new[] { MouseEvent(0x0002), MouseEvent(0x0004) };
        Program.Equal(2u, NativeMethods.SendInput(2, events, Marshal.SizeOf<NativeMethods.Input>()));
    }

    private static NativeMethods.Input MouseEvent(uint flags) => new()
    {
        Type = 0, Data = new NativeMethods.InputUnion { Mouse = new NativeMethods.MouseInput { Flags = flags } }
    };

    private static void ToggleHotkey() => SendKeys((0x11, false), (0x12, false), (0x48, false), (0x48, true), (0x12, true), (0x11, true));

    private static void SendKeys(params (ushort Key, bool Up)[] keys)
    {
        var events = keys.Select(key => new NativeMethods.Input
        {
            Type = 1, Data = new NativeMethods.InputUnion
            {
                Keyboard = new NativeMethods.KeyboardInput { VirtualKey = key.Key, Flags = key.Up ? 2u : 0u }
            }
        }).ToArray();
        NativeMethods.SendInput((uint)events.Length, events, Marshal.SizeOf<NativeMethods.Input>());
    }

    [StructLayout(LayoutKind.Sequential)] private struct Pointer { internal int X; internal int Y; }
    [StructLayout(LayoutKind.Sequential)] private struct WindowBounds { internal int Left; internal int Top; internal int Right; internal int Bottom; }
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetWindowRect(nint handle, out WindowBounds bounds);
    [DllImport("user32.dll")] private static extern nint WindowFromPoint(Pointer point);
    [DllImport("user32.dll")] private static extern nint GetAncestor(nint handle, uint flags);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetForegroundWindow(nint handle);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetCursorPos(out Pointer point);
}
