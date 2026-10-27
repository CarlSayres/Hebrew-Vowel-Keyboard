# Hebrew Marks for Windows

The new **C++ / JUCE Windows and Mac edition** is in [`native/`](native/README.md),
with a GitHub Actions workflow for native Windows and universal Mac test builds.
The instructions below describe the existing, separately maintained WPF version.

A floating, non-activating keyboard for inserting Hebrew niqqud and te'amim into the active editor. Built with C# / WPF on .NET 10. No third-party packages, cloud services, clipboard replacement, or administrator access are needed.

## Run

Open `artifacts/HebrewMarks/HebrewVowelKeyboard.exe` after building. Keep the accompanying DLL, runtime configuration, and dependency files beside the executable. The app requires the .NET 10 **Desktop Runtime**; it is already installed on this development machine. This build is framework-dependent, not a self-contained installer.

1. Open a document in Word or Google Docs and type a Hebrew consonant.
2. Leave the insertion point immediately after that consonant (and any existing marks), in logical text order.
3. Click a mark in the floating palette. The document should keep keyboard focus.
4. Continue typing in the document, or choose another mark for the same letter.

The header shows the **active editor's keyboard language**, updating when you change it in Windows. Use **עברית** or **English** in the header to select an already installed keyboard without taking focus from the editor. If you have multiple layouts for a language, the palette uses the most recently observed layout for that language, otherwise the first installed one. Missing languages have disabled buttons; the app does not install keyboards or change the Windows default language.

Selecting a mark while the editor is using English (or any non-Hebrew keyboard) displays **Select Hebrew keyboard** and inserts nothing. Switch manually using the Hebrew button or Windows, then click the mark again. There is no automatic switching or replay of a refused mark. This avoids the misplaced vowels observed when pointing existing Hebrew text under an English keyboard. If an editor rejects a keyboard change request, use its usual Windows keyboard selector.

Use the **Niqqud** and **Te'amim** buttons in the header to switch pages. **Niqqud** contains 18 points, including meteg and rafe, plus **maqaf** (־, U+05BE), **sof pasuq** (׃, U+05C3), and **paseq** (׀, U+05C0). Its seventh row reads Rafe, Sof pasuq, Paseq from right to left. **Dagesh / Mappiq** inserts U+05BC, the shared character for both uses. **Te'amim** contains the complete U+0591–U+05AF range (including the masora circle). Names follow one common transliteration; traditional names vary.

Both pages show their complete set of buttons with no scrolling. Every row reads **right to left**. Niqqud uses seven rows of up to three buttons; Te'amim uses rows of up to six buttons. The window adjusts its width and height to the selected page, keeping its right edge in place. There is no Quick start page, instructional area, footer, or button tooltip.

Te'amim follows the requested musical groupings, with Qadma and Munah intentionally repeated in two rows. The remaining signs follow the four specified rows in their original catalog order. Niqqud groups vowels and their hataf forms by row, followed by dots and punctuation. Layout is defined separately from character mappings in `Models/PaletteLayout.cs`; displayed spellings and inserted code points are unchanged.

The button **Mapach** is Unicode MAHAPAKH (U+05A4). **Zarqa** uses U+05AE (Unicode's ZINOR), while **Tsinorit** uses U+0598 (Unicode's ZARQA). These display names distinguish the familiar signs despite the [documented Unicode naming anomalies](https://www.unicode.org/notes/tn27/tn27-7.html). Internal command IDs retain their original spellings; the inserted characters are unchanged.

Drag the dark header to move the palette. **Ctrl+Alt+H** hides or shows it. The shortcut is optional: if another program owns it, the app displays a notice and remains visible. Click the X to quit. Avoid launching multiple instances, since only one can own the shortcut.

## Version 0.1 behavior and limitations

- Points and accents use dotted-circle previews, including holam and shin/sin dots. The preview circle is never inserted. Maqaf, sof pasuq and paseq are displayed and inserted as standalone punctuation characters.
- This is insertion-only: it does not inspect document contents, replace existing vowels, normalize existing text, or automatically advance to the next consonant. Selecting text may cause the editor to replace that selection.
- Input goes to the currently focused field. Click inside the document before using the palette; a browser's address bar or search box can also receive text.
- Normal insertion leaves the header unchanged. If Windows refuses input, a brief error replaces the shortcut hint. Input delivery does not confirm that the editor changed the document.
- Release Ctrl, Alt, Shift and Windows keys before clicking a mark. The app refuses insertion while these modifiers are held.
- Run the editor at normal permissions. Windows can block input into an elevated app.
- Correct mark placement depends on the document font and editor. Several marks on one letter, especially Biblical Hebrew combinations, need a font that supports them.
- Undo, backspace and cursor movement belong to the editor; behavior may differ between applications. There is no simulated "remove last vowel" button.
- Window position is not yet saved. Physical keypad/MIDI input, remapping, adjustable button size and additional punctuation are future work.

## Build and test

Use a Windows machine with the .NET 10 SDK:

```powershell
.\build.ps1
```

For interactive integration tests, close any running palette and run:

```powershell
.\build.ps1 -Integration
```

The integration test briefly opens its own disposable editor, moves the mouse, clicks the actual palette, and sends synthetic input. Run it on an unlocked desktop and leave the mouse and keyboard idle until it finishes. It does not use existing user documents. A restricted sandbox may block `SendInput`; run this test in a normal desktop process.

The package-free test runner checks native structure layout, keyboard language identification, exact code points, paired Unicode key events, catalog coverage, real cross-process insertion, non-activation, panel switching, modifier handling, and show/hide behavior. The interactive test requires installed Hebrew and English keyboards and checks the language indicator, explicit keyboard selection, and refusal to insert in English. Render UI previews with:

```powershell
.\tests\HebrewVowelKeyboard.Tests\bin\Release\net10.0-windows\HebrewVowelKeyboard.Tests.exe --render
```

Previews are written to `artifacts/previews/`.

## Application acceptance checks

The user confirmed insertion works in Word and Google Docs with a Hebrew keyboard selected. The disposable WPF editor test verifies the Windows mechanism, but changes still need application-specific acceptance checks. Test the following in a new scratch document in desktop Word, then Google Docs in Edge and Chrome:

1. Type shin, then use qamats, dagesh, shin dot and etnahta. Confirm the expected combined glyph and that ordinary keyboard input still reaches the document.
2. Try each mark on its own consonant and test both niqqud and te'amim panels. Check maqaf between words.
3. Point an existing word. Check right-to-left caret placement at the beginning, middle and end of a line, including mixed Hebrew/English text.
4. Test undo, redo and backspace; record whether each operation affects a mark or a whole cluster.
5. Switch away and back, drag the palette, then hide/show with Ctrl+Alt+H. Confirm focus is retained when clicking marks.
6. Check that both complete pages fit without scrolling, including on high-DPI displays, and use a font suitable for Biblical Hebrew.
7. Select English, click a vowel, and confirm a warning appears without changing the document. Select Hebrew using the palette, then click again. Confirm the header follows keyboard changes made outside the palette too.

## Structure

- `Models/MarkCatalog.cs`: stable command IDs and Unicode mappings.
- `Input/IMarkOutput` (in `UnicodeInputSender.cs`): output boundary shared by future input sources.
- `Input/UnicodeInputSender.cs`: sends Unicode key-down/key-up pairs to the foreground application; does not steal focus or manipulate the clipboard.
- `Input/KeyboardLayouts.cs`: reads the active editor's input language and requests a layout change only when a language button is clicked.
- `MainWindow`: mouse palette; `WS_EX_NOACTIVATE` plus `WM_MOUSEACTIVATE` handling keeps the editor active.
- `tests`: executable checks without external test packages, plus a disposable editor for integration tests.

A future keypad or MIDI adapter can map events to a catalog ID and call the same output service. A normal USB keypad additionally needs a deliberate strategy to suppress its ordinary digits. MIDI notes should trigger on note-on only, with note-off/zero-velocity handling to avoid duplicate marks.

## References

- [Microsoft: Unicode keyboard input](https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-keybdinput)
- [Microsoft: SendInput and integrity restrictions](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput)
- [Microsoft: Mouse activation](https://learn.microsoft.com/en-us/windows/win32/inputdev/about-mouse-input)
- [Unicode: Hebrew script](https://www.unicode.org/versions/Unicode16.0.0/core-spec/chapter-9/)
