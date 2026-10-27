# Hebrew Marks native test build

This app needs no .NET installation. This is a new native edition; the original
Windows WPF app's successful Word/Google Docs tests do not validate this edition.

## On the MacBook

1. Download the **HebrewMarks-macOS-universal** artifact from a successful GitHub
   Actions run. Extract the downloaded archive and the package ZIP inside it.
2. Drag **Hebrew Marks.app** into **Applications** before granting permissions.
   Use that copy consistently; moving it afterward can affect permissions.
3. Open it. This test build has an ad-hoc signature, not a verified Apple
   Developer ID. If macOS blocks it, use **System Settings > Privacy & Security >
   Open Anyway**, then confirm opening your own test build. Do not disable
   Gatekeeper globally. If there is no override, record the exact message.
4. In the palette, click **Allow access**, then enable Hebrew Marks under
   **Privacy & Security > Accessibility**. Close and reopen the app if requested.
5. Add Hebrew and English input sources in macOS Keyboard settings if needed.
6. Open a new scratch document in Word. Select Hebrew and type a consonant. Leave
   the caret after it, then click a mark. The document should retain focus.

Use **Control-Option-H** to show/hide on Mac (**Ctrl-Alt-H** on Windows).
Drag the dark header to move. The X quits. Close the old Windows palette before
running this native version, since both use the same shortcut.

## Acceptance checks

Repeat in Word and Google Docs (note the browser and macOS version):

- Insert qamats, dagesh, shin dot and etnahta on shin. The dotted circle should
  appear only in the palette, never in the document.
- Try all buttons, including maqaf, sof pasuq, paseq and the Te'amim page.
- Add vowels to existing Hebrew, including near mixed English/Hebrew text.
- Under English, clicking a mark must warn and leave the document unchanged.
- Click Hebrew in the palette: confirm the input source changes without moving
  the caret. Click the mark again; refused marks must never replay themselves.
- Verify the header follows input-source changes made with the system selector.
- Hold Shift/Control/Option/Command: mark insertion must be refused.
- Switch pages, drag, hide/show, then type using the physical keyboard. The editor
  should remain the typing target.
- Check undo/backspace, all button labels and dotted-circle previews, both full
  pages without scrolling, and display scaling.

If a language-selection button fails, try the macOS input menu and report it.
Record app/browser versions and a short reproduction; don't use private documents
for diagnostics. The Mac input backend needs these real-editor checks before release.
