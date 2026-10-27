using System.Globalization;
using System.Runtime.InteropServices;
using HebrewVowelKeyboard.Input;
using HebrewVowelKeyboard.Models;

namespace HebrewVowelKeyboard.Tests;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Contains("--host")) return IntegrationTests.RunHost();
        if (args.Length == 2 && args[0] == "--native") return NativeIntegrationTests.Run(args[1]);
        if (args.Contains("--integration")) return IntegrationTests.Run();
        if (args.Contains("--render")) return PreviewRenderer.Run();
        var tests = new (string Name, Action Check)[]
        {
            ("Native INPUT layout matches Windows ABI", () =>
            {
                Equal(IntPtr.Size == 8 ? 40 : 28, Marshal.SizeOf<NativeMethods.Input>());
                Equal(IntPtr.Size == 8 ? 8 : 4, (int)Marshal.OffsetOf<NativeMethods.Input>("Data"));
                Equal(IntPtr.Size == 8 ? 72 : 48, Marshal.SizeOf<NativeMethods.GuiThreadInfo>());
            }),
            ("Keyboard language detection accepts Hebrew variants and rejects English or unknown", () =>
            {
                Equal(true, KeyboardLayouts.IsHebrew((nint)0x040D040D));
                Equal(true, KeyboardLayouts.IsHebrew(unchecked((nint)0xFFFFFFFFF002040D)));
                Equal(false, KeyboardLayouts.IsHebrew((nint)0x04090409));
                Equal(false, KeyboardLayouts.IsHebrew(0));
            }),
            ("Every catalog button sends only its Unicode character", () =>
            {
                Equal(MarkCatalog.All.Count, MarkCatalog.All.Select(mark => mark.Id).Distinct().Count());
                Equal(MarkCatalog.All.Count, MarkCatalog.All.Select(mark => mark.Character).Distinct().Count());
                foreach (var mark in MarkCatalog.All)
                {
                    Equal(mark.Id switch
                        {
                            "maqaf" => UnicodeCategory.DashPunctuation,
                            "sof-pasuq" or "paseq" => UnicodeCategory.OtherPunctuation,
                            _ => UnicodeCategory.NonSpacingMark
                        },
                        char.GetUnicodeCategory(mark.Character));
                    Equal(1, mark.Text.Length);
                    var events = UnicodeInputSender.CreateInputs(mark.Text);
                    Equal(2, events.Length);
                    Equal(1u, events[0].Type);
                    Equal((ushort)0, events[0].Data.Keyboard.VirtualKey);
                    Equal((ushort)mark.Character, events[0].Data.Keyboard.Scan);
                    Equal(0x0004u, events[0].Data.Keyboard.Flags);
                    Equal((ushort)mark.Character, events[1].Data.Keyboard.Scan);
                    Equal(0x0006u, events[1].Data.Keyboard.Flags);
                }
            }),
            ("Punctuation inserts standalone characters and mappiq uses dagesh", () =>
            {
                Equal("\u05BE", MarkCatalog.Find("maqaf").Text);
                Equal("\u05BE", MarkCatalog.Find("maqaf").Preview);
                Equal("\u05C3", MarkCatalog.Find("sof-pasuq").Text);
                Equal("\u05C3", MarkCatalog.Find("sof-pasuq").Preview);
                Equal("\u05C0", MarkCatalog.Find("paseq").Text);
                Equal("\u05C0", MarkCatalog.Find("paseq").Preview);
                Equal("\u05BC", MarkCatalog.Find("dagesh").Text);
            }),
            ("Complete Unicode Hebrew accent and masora range is present", () =>
                Equal(string.Concat(Enumerable.Range(0x0591, 31).Select(value => (char)value)),
                    string.Concat(MarkCatalog.All.Where(mark => mark.Group == "Te'amim").Select(mark => mark.Character)))),
            ("Multiple marks retain input order and paired key releases", () =>
            {
                var events = UnicodeInputSender.CreateInputs("\u05B8\u05BC\u0591");
                Equal(6, events.Length);
                Equal("\u05B8\u05BC\u0591", string.Concat(events.Where((_, index) => index % 2 == 0).Select(input => (char)input.Data.Keyboard.Scan)));
            })
        };
        foreach (var test in tests)
        {
            try { test.Check(); Console.WriteLine($"PASS {test.Name}"); }
            catch (Exception ex) { Console.Error.WriteLine($"FAIL {test.Name}: {ex.Message}"); return 1; }
        }
        Console.WriteLine($"{tests.Length} checks passed.");
        return 0;
    }

    internal static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected {expected}; received {actual}.");
    }
}
