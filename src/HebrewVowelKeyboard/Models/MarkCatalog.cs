namespace HebrewVowelKeyboard.Models;

public static class MarkCatalog
{
    public static IReadOnlyList<HebrewMark> All { get; } = Array.AsReadOnly(new HebrewMark[]
    {
        new("sheva", "Sheva", "שווא", '\u05B0', "Niqqud"),
        new("hataf-segol", "Hataf segol", "חטף סגול", '\u05B1', "Niqqud"),
        new("hataf-patah", "Hataf patah", "חטף פתח", '\u05B2', "Niqqud"),
        new("hataf-qamats", "Hataf qamats", "חטף קמץ", '\u05B3', "Niqqud"),
        new("hiriq", "Hiriq", "חיריק", '\u05B4', "Niqqud"),
        new("tsere", "Tsere", "צירי", '\u05B5', "Niqqud"),
        new("segol", "Segol", "סגול", '\u05B6', "Niqqud"),
        new("patah", "Patah", "פתח", '\u05B7', "Niqqud"),
        new("qamats", "Qamats", "קמץ", '\u05B8', "Niqqud"),
        new("holam", "Holam", "חולם", '\u05B9', "Niqqud"),
        new("holam-haser", "Holam haser", "חולם חסר", '\u05BA', "Niqqud"),
        new("qubuts", "Qubuts", "קובוץ", '\u05BB', "Niqqud"),
        new("dagesh", "Dagesh / Mappiq", "דגש / מפיק", '\u05BC', "Niqqud"),
        new("meteg", "Meteg", "מתג", '\u05BD', "Niqqud"),
        new("rafe", "Rafe", "רפה", '\u05BF', "Niqqud"),
        new("shin-dot", "Shin dot", "נקודת שין", '\u05C1', "Niqqud"),
        new("sin-dot", "Sin dot", "נקודת שׂין", '\u05C2', "Niqqud"),
        new("qamats-qatan", "Qamats qatan", "קמץ קטן", '\u05C7', "Niqqud"),
        new("maqaf", "Maqaf", "מקף", '\u05BE', "Niqqud"),
        new("sof-pasuq", "Sof pasuq", "סוף פסוק", '\u05C3', "Niqqud"),
        new("paseq", "Paseq", "פסק", '\u05C0', "Niqqud"),
        new("etnahta", "Etnahta", "אתנחתא", '\u0591', "Te'amim"),
        new("segolta", "Segolta", "סגולתא", '\u0592', "Te'amim"),
        new("shalshelet", "Shalshelet", "שלשלת", '\u0593', "Te'amim"),
        new("zaqef-qatan", "Zaqef qatan", "זקף קטן", '\u0594', "Te'amim"),
        new("zaqef-gadol", "Zaqef gadol", "זקף גדול", '\u0595', "Te'amim"),
        new("tipeha", "Tipeha", "טפחא", '\u0596', "Te'amim"),
        new("revia", "Revia", "רביע", '\u0597', "Te'amim"),
        // Preserve command IDs and code points; display the familiar cantillation
        // names rather than Unicode's anomalous ZARQA / ZINOR character names.
        new("zarqa", "Tsinorit", "צינורית", '\u0598', "Te'amim"),
        new("pashta", "Pashta", "פשטא", '\u0599', "Te'amim"),
        new("yetiv", "Yetiv", "יתיב", '\u059A', "Te'amim"),
        new("tevir", "Tevir", "תביר", '\u059B', "Te'amim"),
        new("geresh-accent", "Geresh", "גרש", '\u059C', "Te'amim"),
        new("geresh-muqdam", "Geresh muqdam", "גרש מוקדם", '\u059D', "Te'amim"),
        new("gershayim-accent", "Gershayim", "גרשיים", '\u059E', "Te'amim"),
        new("qarney-para", "Qarney para", "קרני פרה", '\u059F', "Te'amim"),
        new("telisha-gedola", "Telisha gedola", "תלישא גדולה", '\u05A0', "Te'amim"),
        new("pazer", "Pazer", "פזר", '\u05A1', "Te'amim"),
        new("atnah-hafukh", "Atnah hafukh", "אתנח הפוך", '\u05A2', "Te'amim"),
        new("munah", "Munah", "מונח", '\u05A3', "Te'amim"),
        new("mahapakh", "Mapach", "מהפך", '\u05A4', "Te'amim"),
        new("merkha", "Merkha", "מרכא", '\u05A5', "Te'amim"),
        new("merkha-kefula", "Merkha kefula", "מרכא כפולה", '\u05A6', "Te'amim"),
        new("darga", "Darga", "דרגא", '\u05A7', "Te'amim"),
        new("qadma", "Qadma", "קדמא", '\u05A8', "Te'amim"),
        new("telisha-qetana", "Telisha qetana", "תלישא קטנה", '\u05A9', "Te'amim"),
        new("yerah-ben-yomo", "Yerah ben yomo", "ירח בן יומו", '\u05AA', "Te'amim"),
        new("ole", "Ole", "עולה", '\u05AB', "Te'amim"),
        new("iluy", "Iluy", "עילוי", '\u05AC', "Te'amim"),
        new("dehi", "Dehi", "דחי", '\u05AD', "Te'amim"),
        new("zinor", "Zarqa", "זרקא", '\u05AE', "Te'amim"),
        new("masora-circle", "Masora circle", "עיגול מסורה", '\u05AF', "Te'amim"),
    });

    public static HebrewMark Find(string id) => All.First(mark => mark.Id == id);
}
