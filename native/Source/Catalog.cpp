#include "Catalog.h"
#include <stdexcept>
#include <set>

const std::vector<Mark>& catalog()
{
    static const std::vector<Mark> marks {
        { "sheva", "Sheva", "שווא", 0x05B0, true },
        { "hataf-segol", "Hataf segol", "חטף סגול", 0x05B1, true },
        { "hataf-patah", "Hataf patah", "חטף פתח", 0x05B2, true },
        { "hataf-qamats", "Hataf qamats", "חטף קמץ", 0x05B3, true },
        { "hiriq", "Hiriq", "חיריק", 0x05B4, true },
        { "tsere", "Tsere", "צירי", 0x05B5, true },
        { "segol", "Segol", "סגול", 0x05B6, true },
        { "patah", "Patah", "פתח", 0x05B7, true },
        { "qamats", "Qamats", "קמץ", 0x05B8, true },
        { "holam", "Holam", "חולם", 0x05B9, true },
        { "holam-haser", "Holam haser", "חולם חסר", 0x05BA, true },
        { "qubuts", "Qubuts", "קובוץ", 0x05BB, true },
        { "dagesh", "Dagesh / Mappiq", "דגש / מפיק", 0x05BC, true },
        { "meteg", "Meteg", "מתג", 0x05BD, true },
        { "rafe", "Rafe", "רפה", 0x05BF, true },
        { "shin-dot", "Shin dot", "נקודת שין", 0x05C1, true },
        { "sin-dot", "Sin dot", "נקודת שׂין", 0x05C2, true },
        { "qamats-qatan", "Qamats qatan", "קמץ קטן", 0x05C7, true },
        { "maqaf", "Maqaf", "מקף", 0x05BE, true },
        { "sof-pasuq", "Sof pasuq", "סוף פסוק", 0x05C3, true },
        { "paseq", "Paseq", "פסק", 0x05C0, true },
        { "etnahta", "Etnahta", "אתנחתא", 0x0591, false },
        { "segolta", "Segolta", "סגולתא", 0x0592, false },
        { "shalshelet", "Shalshelet", "שלשלת", 0x0593, false },
        { "zaqef-qatan", "Zaqef qatan", "זקף קטן", 0x0594, false },
        { "zaqef-gadol", "Zaqef gadol", "זקף גדול", 0x0595, false },
        { "tipeha", "Tipeha", "טפחא", 0x0596, false },
        { "revia", "Revia", "רביע", 0x0597, false },
        { "zarqa", "Tsinorit", "צינורית", 0x0598, false },
        { "pashta", "Pashta", "פשטא", 0x0599, false },
        { "yetiv", "Yetiv", "יתיב", 0x059A, false },
        { "tevir", "Tevir", "תביר", 0x059B, false },
        { "geresh-accent", "Geresh", "גרש", 0x059C, false },
        { "geresh-muqdam", "Geresh muqdam", "גרש מוקדם", 0x059D, false },
        { "gershayim-accent", "Gershayim", "גרשיים", 0x059E, false },
        { "qarney-para", "Qarney para", "קרני פרה", 0x059F, false },
        { "telisha-gedola", "Telisha gedola", "תלישא גדולה", 0x05A0, false },
        { "pazer", "Pazer", "פזר", 0x05A1, false },
        { "atnah-hafukh", "Atnah hafukh", "אתנח הפוך", 0x05A2, false },
        { "munah", "Munah", "מונח", 0x05A3, false },
        { "mahapakh", "Mapach", "מהפך", 0x05A4, false },
        { "merkha", "Merkha", "מרכא", 0x05A5, false },
        { "merkha-kefula", "Merkha kefula", "מרכא כפולה", 0x05A6, false },
        { "darga", "Darga", "דרגא", 0x05A7, false },
        { "qadma", "Qadma", "קדמא", 0x05A8, false },
        { "telisha-qetana", "Telisha qetana", "תלישא קטנה", 0x05A9, false },
        { "yerah-ben-yomo", "Yerah ben yomo", "ירח בן יומו", 0x05AA, false },
        { "ole", "Ole", "עולה", 0x05AB, false },
        { "iluy", "Iluy", "עילוי", 0x05AC, false },
        { "dehi", "Dehi", "דחי", 0x05AD, false },
        { "zinor", "Zarqa", "זרקא", 0x05AE, false },
        { "masora-circle", "Masora circle", "עיגול מסורה", 0x05AF, false },
    };
    return marks;
}

const Mark& findMark(const std::string& id)
{
    for (const auto& mark : catalog()) if (id == mark.id) return mark;
    throw std::invalid_argument("Unknown mark: " + id);
}

const Rows& niqqudRows()
{
    static const Rows rows {
        { "qamats", "patah", "hataf-patah" },
        { "tsere", "segol", "hataf-segol" },
        { "holam-haser", "qamats-qatan", "hataf-qamats" },
        { "sheva", "hiriq", "qubuts" },
        { "shin-dot", "sin-dot", "holam" },
        { "dagesh", "meteg", "maqaf" },
        { "rafe", "sof-pasuq", "paseq" }
    };
    return rows;
}
const Rows& teamimRows()
{
    static const Rows rows = []
    {
        Rows r {
            { "merkha", "tipeha", "munah", "etnahta", "darga", "tevir" },
            { "qadma", "mahapakh", "pashta", "munah", "zaqef-qatan", "zaqef-gadol" },
            { "qadma", "geresh-muqdam", "geresh-accent", "gershayim-accent", "telisha-qetana", "telisha-gedola" },
            { "revia", "zinor", "segolta", "yetiv", "pazer", "shalshelet" }
        };
        std::set<std::string> placed;
        for (const auto& row : r) for (const auto& id : row) placed.insert(id);
        std::vector<std::string> extra;
        for (const auto& mark : catalog())
            if (!mark.niqqud && placed.count(mark.id)==0)
            {
                extra.push_back(mark.id);
                if (extra.size()==6) { r.push_back(extra); extra.clear(); }
            }
        if (!extra.empty()) r.push_back(extra);
        return r;
    }();
    return rows;
}
bool isPunctuation(char16_t c) { return c==0x05be || c==0x05c0 || c==0x05c3; }
