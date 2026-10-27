using System.Runtime.InteropServices;
using HebrewVowelKeyboard.Models;

namespace HebrewVowelKeyboard.Input;

public sealed record InsertionResult(bool Success, string Message);

public interface IMarkOutput
{
    InsertionResult Insert(HebrewMark mark);
}

public sealed class UnicodeInputSender : IMarkOutput
{
    public InsertionResult Insert(HebrewMark mark)
    {
        var target = KeyboardLayouts.CurrentTarget();
        if (target is null)
            return new(false, "Click inside your document first.");

        // Never release the user's modifiers or send delayed input to a stale target.
        if (new[] { 0x10, 0x11, 0x12, 0x5B, 0x5C }.Any(key => (NativeMethods.GetAsyncKeyState(key) & 0x8000) != 0))
            return new(false, "Release Shift, Ctrl, Alt and Windows, then try again.");

        if (!KeyboardLayouts.IsHebrew(target.Layout))
            return new(false, "Select Hebrew keyboard");

        var inputs = CreateInputs(mark.Text);
        var current = KeyboardLayouts.CurrentTarget();
        if (!KeyboardLayouts.SameTarget(target, current))
            return new(false, "The active window changed. Click your document and try again.");
        if (!KeyboardLayouts.IsHebrew(current!.Layout))
            return new(false, "Select Hebrew keyboard");

        var sent = NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.Input>());
        return sent == inputs.Length
            ? new(true, $"Sent {mark.Name} · {mark.CodePoint}")
            : new(false, "Input was blocked. Keep your editor and this app at normal permissions.");
    }

    internal static NativeMethods.Input[] CreateInputs(string text) => text.SelectMany(character => new[]
    {
        CreateKey(character, NativeMethods.KeyeventfUnicode),
        CreateKey(character, NativeMethods.KeyeventfUnicode | NativeMethods.KeyeventfKeyup)
    }).ToArray();

    private static NativeMethods.Input CreateKey(char character, uint flags) => new()
    {
        Type = 1,
        Data = new NativeMethods.InputUnion
        {
            Keyboard = new NativeMethods.KeyboardInput { Scan = character, Flags = flags }
        }
    };
}
