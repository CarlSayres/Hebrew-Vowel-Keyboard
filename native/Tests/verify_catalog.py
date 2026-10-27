"""Catch accidental code-point/name/row changes during the C# -> C++ port."""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[2]
old = (root / "src/HebrewVowelKeyboard/Models/MarkCatalog.cs").read_text(encoding="utf-8-sig")
new = (root / "native/Source/Catalog.cpp").read_text(encoding="utf-8-sig")
old_marks = re.findall(r'new\("([^"]+)", "([^"]+)", "([^"]+)", \'\\u([0-9A-Fa-f]+)\', "([^"]+)"\)', old)
new_marks = re.findall(r'\{ "([^"]+)", "([^"]+)", "([^"]+)", 0x([0-9A-Fa-f]+), (true|false) \}', new)
expected = [(i, n, h, int(c, 16), g == "Niqqud") for i, n, h, c, g in old_marks]
actual = [(i, n, h, int(c, 16), g == "true") for i, n, h, c, g in new_marks]
if len(expected) != 52 or actual != expected:
    raise SystemExit("FAIL: native catalog differs from the tested Windows catalog")

old_rows = (root / "src/HebrewVowelKeyboard/Models/PaletteLayout.cs").read_text(encoding="utf-8-sig")
expected_rows = re.findall(r'\["[a-z-]+"[^\]\n]*\]', old_rows)
actual_rows = re.findall(r'\{ ("[a-z-]+"(?:, "[a-z-]+")*) \}', new)
extract = lambda rows: [re.findall(r'"([a-z-]+)"', r) for r in rows]
if len(expected_rows) != 11 or extract(actual_rows) != extract(expected_rows):
    raise SystemExit("FAIL: native primary rows differ from the requested RTL layout")
print("PASS: all 52 IDs, names, Hebrew labels, code points and 11 primary rows match")
