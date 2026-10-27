namespace HebrewVowelKeyboard.Models;

// Input adapters use the ID; UI labels and device mappings can change independently.
public sealed record HebrewMark(string Id, string Name, string HebrewName, char Character, string Group)
{
    public string Text => Character.ToString();
    public string Preview => Character is '\u05BE' or '\u05C0' or '\u05C3' ? Text : "\u25CC" + Character;
    public string CodePoint => $"U+{(int)Character:X4}";
    public string Description => $"{Name} · {HebrewName} · {CodePoint}";
}
