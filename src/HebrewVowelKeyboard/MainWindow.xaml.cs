using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using HebrewVowelKeyboard.Input;
using HebrewVowelKeyboard.Models;

namespace HebrewVowelKeyboard;

public partial class MainWindow : Window
{
    private const int HotkeyId = 0x484D;
    private readonly IMarkOutput output;
    private HwndSource? source;
    private bool hotkeyRegistered;
    private readonly DispatcherTimer keyboardTimer = new() { Interval = TimeSpan.FromMilliseconds(300) };
    private readonly Dictionary<int, nint> lastLayouts = new();
    private bool selectingKeyboard;
    private int noticeVersion;
    internal bool IsHotkeyRegistered => hotkeyRegistered;

    public MainWindow() : this(new UnicodeInputSender()) { }

    internal MainWindow(IMarkOutput output)
    {
        this.output = output;
        InitializeComponent();
        SelectGroup("Niqqud");
        var work = SystemParameters.WorkArea;
        Left = Math.Max(work.Left, work.Right - Width - 24);
        Top = work.Top + 40;
        SizeChanged += (_, _) => Top = Math.Max(work.Top, Math.Min(Top, work.Bottom - ActualHeight));
        SourceInitialized += InitializeNativeWindow;
        keyboardTimer.Tick += (_, _) => RefreshKeyboard();
        Loaded += (_, _) => { RefreshKeyboard(); keyboardTimer.Start(); };
        Closed += OnClosed;
    }

    private void InitializeNativeWindow(object? sender, EventArgs args)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var style = NativeMethods.GetWindowLong(hwnd, NativeMethods.GwlExStyle);
        NativeMethods.SetWindowLong(hwnd, NativeMethods.GwlExStyle, (nint)((long)style | NativeMethods.WsExNoActivate));
        source = HwndSource.FromHwnd(hwnd);
        source.AddHook(WindowMessage);
        // MOD_CONTROL | MOD_ALT | MOD_NOREPEAT
        hotkeyRegistered = NativeMethods.RegisterHotKey(hwnd, HotkeyId, 0x4003, 0x48);
        if (!hotkeyRegistered)
            ShortcutText.Text = "Ctrl-Alt-H unavailable";
    }

    private nint WindowMessage(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message == NativeMethods.WmMouseActivate)
        {
            handled = true;
            return 3; // MA_NOACTIVATE: process click, keep the editor active.
        }

        if (message == NativeMethods.WmHotkey && (int)wParam == HotkeyId)
        {
            handled = true;
            if (IsVisible) Hide(); else Show();
        }
        return 0;
    }

    private void InsertMark(object sender, RoutedEventArgs args)
    {
        if (((Button)sender).Tag is not HebrewMark mark) return;
        if (selectingKeyboard) { ShowNotice("Wait for keyboard change"); return; }
        var result = output.Insert(mark);
        // Keep the compact header quiet during normal input. A failed insertion
        // briefly replaces the shortcut hint without opening a focus-stealing dialog.
        if (!result.Success)
        {
            ShowNotice(result.Message);
        }
        else ClearNotice();
        RefreshKeyboard();
    }

    private async void ShowNotice(string message)
    {
        var version = ++noticeVersion;
        ShortcutText.Text = message;
        ShortcutText.Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 133));
        await System.Threading.Tasks.Task.Delay(6000);
        if (noticeVersion == version) ClearNotice();
    }

    private void ClearNotice()
    {
        noticeVersion++;
        ShortcutText.Text = hotkeyRegistered ? "Ctrl-Alt-H to show or hide" : "Ctrl-Alt-H unavailable";
        ShortcutText.Foreground = new SolidColorBrush(Color.FromRgb(178, 219, 210));
    }

    private void RefreshKeyboard()
    {
        var target = KeyboardLayouts.CurrentTarget();
        var installed = KeyboardLayouts.Installed();
        if (target is not null && target.Layout != 0)
            lastLayouts[KeyboardLayouts.PrimaryLanguage(target.Layout)] = target.Layout;
        KeyboardStatus.Text = target is null ? "Keyboard: click editor" : $"Keyboard: {KeyboardLayouts.LanguageName(target.Layout)}";
        KeyboardStatus.Foreground = new SolidColorBrush(target is not null && !KeyboardLayouts.IsHebrew(target.Layout)
            ? Color.FromRgb(255, 215, 133) : Color.FromRgb(178, 219, 210));
        SetKeyboardButton(HebrewKeyboardButton, KeyboardLayouts.Hebrew);
        SetKeyboardButton(EnglishKeyboardButton, KeyboardLayouts.English);

        void SetKeyboardButton(Button button, int language)
        {
            button.IsEnabled = !selectingKeyboard && target is not null && installed.Any(layout => KeyboardLayouts.PrimaryLanguage(layout) == language);
            var selected = target is not null && KeyboardLayouts.PrimaryLanguage(target.Layout) == language;
            button.Background = new SolidColorBrush(selected ? Color.FromRgb(219, 239, 233) : Colors.White);
            button.BorderBrush = new SolidColorBrush(selected ? Color.FromRgb(33, 134, 117) : Color.FromRgb(220, 227, 232));
            button.FontWeight = selected ? FontWeights.SemiBold : FontWeights.Normal;
        }
    }

    private async void SelectHebrewKeyboard(object sender, RoutedEventArgs args) => await SelectKeyboard(KeyboardLayouts.Hebrew);
    private async void SelectEnglishKeyboard(object sender, RoutedEventArgs args) => await SelectKeyboard(KeyboardLayouts.English);

    private async Task SelectKeyboard(int language)
    {
        if (selectingKeyboard) return;
        var target = KeyboardLayouts.CurrentTarget();
        if (target is null) { ShowNotice("Click your document first"); return; }
        if (KeyboardLayouts.PrimaryLanguage(target.Layout) == language) { ClearNotice(); RefreshKeyboard(); return; }
        var installed = KeyboardLayouts.Installed();
        var layout = lastLayouts.TryGetValue(language, out var remembered) && installed.Contains(remembered)
            ? remembered : installed.FirstOrDefault(value => KeyboardLayouts.PrimaryLanguage(value) == language);
        if (layout == 0) { ShowNotice("Keyboard not installed"); return; }
        if (!KeyboardLayouts.RequestSelection(target, layout)) { ShowNotice("Switch keyboard in editor"); return; }
        selectingKeyboard = true;
        RefreshKeyboard();
        try
        {
            var deadline = Environment.TickCount64 + 1000;
            while (Environment.TickCount64 < deadline)
            {
                await Task.Delay(30);
                var current = KeyboardLayouts.CurrentTarget();
                if (!KeyboardLayouts.SameTarget(target, current)) { ShowNotice("Click your document first"); return; }
                if (current!.Layout == layout) { ClearNotice(); return; }
            }
            ShowNotice("Switch keyboard in editor");
        }
        finally { selectingKeyboard = false; RefreshKeyboard(); }
    }

    private void ChangeGroup(object sender, RoutedEventArgs args) => SelectGroup((string)((Button)sender).Tag);

    private void SelectGroup(string group)
    {
        var rightEdge = Left + Width;
        Width = group == "Niqqud" ? 306 : 594;
        if (double.IsFinite(rightEdge)) Left = rightEdge - Width;
        MarkButtons.ItemsSource = group == "Niqqud" ? PaletteLayout.Niqqud : PaletteLayout.Teamim;
        foreach (var tab in new[] { NiqqudTab, TeamimTab })
        {
            var selected = (string)tab.Tag == group;
            tab.Background = new SolidColorBrush(selected ? Color.FromRgb(219, 239, 233) : Colors.White);
            tab.BorderBrush = new SolidColorBrush(selected ? Color.FromRgb(33, 134, 117) : Color.FromRgb(220, 227, 232));
            tab.FontWeight = selected ? FontWeights.SemiBold : FontWeights.Normal;
        }
    }

    private void DragHeader(object sender, MouseButtonEventArgs args)
    {
        if (args.OriginalSource is not Border and not Grid and not StackPanel) return;
        NativeMethods.ReleaseCapture();
        NativeMethods.SendMessage(new WindowInteropHelper(this).Handle, 0x00A1, 2, 0); // WM_NCLBUTTONDOWN / HTCAPTION
    }

    private void ClosePalette(object sender, RoutedEventArgs args) => Close();

    private void OnClosed(object? sender, EventArgs args)
    {
        keyboardTimer.Stop();
        noticeVersion++;
        if (hotkeyRegistered) NativeMethods.UnregisterHotKey(new WindowInteropHelper(this).Handle, HotkeyId);
        source?.RemoveHook(WindowMessage);
    }
}
