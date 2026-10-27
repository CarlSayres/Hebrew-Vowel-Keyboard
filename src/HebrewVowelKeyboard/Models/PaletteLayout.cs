namespace HebrewVowelKeyboard.Models;

// Each row is listed in reading order, from right to left. Repeated IDs are
// intentional shortcuts in different musical groups, not duplicate characters.
public static class PaletteLayout
{
    public static IReadOnlyList<IReadOnlyList<HebrewMark>> Niqqud { get; } = Rows(
        ["qamats", "patah", "hataf-patah"],
        ["tsere", "segol", "hataf-segol"],
        ["holam-haser", "qamats-qatan", "hataf-qamats"],
        ["sheva", "hiriq", "qubuts"],
        ["shin-dot", "sin-dot", "holam"],
        ["dagesh", "meteg", "maqaf"],
        ["rafe", "sof-pasuq", "paseq"]);

    public static IReadOnlyList<IReadOnlyList<HebrewMark>> Teamim { get; } = CreateTeamim();

    private static IReadOnlyList<IReadOnlyList<HebrewMark>> CreateTeamim()
    {
        string[][] primary =
        [
            ["merkha", "tipeha", "munah", "etnahta", "darga", "tevir"],
            ["qadma", "mahapakh", "pashta", "munah", "zaqef-qatan", "zaqef-gadol"],
            ["qadma", "geresh-muqdam", "geresh-accent", "gershayim-accent", "telisha-qetana", "telisha-gedola"],
            ["revia", "zinor", "segolta", "yetiv", "pazer", "shalshelet"]
        ];
        var placed = primary.SelectMany(row => row).ToHashSet();
        var remaining = MarkCatalog.All.Where(mark => mark.Group == "Te'amim" && !placed.Contains(mark.Id))
            .Select(mark => mark.Id).Chunk(6);
        return Rows(primary.Concat(remaining).ToArray());
    }

    private static IReadOnlyList<IReadOnlyList<HebrewMark>> Rows(params string[][] rows) =>
        Array.AsReadOnly(rows.Select(row => (IReadOnlyList<HebrewMark>)Array.AsReadOnly(row.Select(MarkCatalog.Find).ToArray())).ToArray());
}
