# Hebrew Marks native edition

C++ / JUCE port for Windows and macOS. The existing C# app remains in `src/`.
This edition shares the mark catalog, palette and command interface. The native
Windows/macOS adapters handle the floating window, keyboard selection and Unicode
insertion. MIDI and physical-keypad mapping are still future work.

## Build locally

Requires CMake 3.24+, a C++17 compiler and an internet connection for the first
configure. JUCE 9.0.4 is pinned to a commit and verified by SHA-256.

Windows: install Visual Studio's Desktop development with C++ workload, then:

```powershell
cmake -S native -B native/build -A x64
cmake --build native/build --config Release --parallel 3
ctest --test-dir native/build -C Release --output-on-failure
```

The executable is `native/build/HebrewMarks_artefacts/Release/Hebrew Marks.exe`.
The C++ runtime is linked statically; no .NET or separate Visual C++ runtime
installation is required. Normal Windows system DLLs are still used.

Mac: with Xcode command-line tools and CMake installed:

```sh
cmake -S native -B native/build -DCMAKE_BUILD_TYPE=Release '-DCMAKE_OSX_ARCHITECTURES=arm64;x86_64' -DCMAKE_OSX_DEPLOYMENT_TARGET=11.0
cmake --build native/build --parallel 3
ctest --test-dir native/build --output-on-failure
```

## GitHub builds

`.github/workflows/native-build.yml` builds Windows x64 and a universal Mac app
(Apple Silicon + Intel) on relevant pushes to `main`, pull requests, or a manual
**Actions > Native Windows and Mac builds > Run workflow** request.

Each successful job uploads a ZIP and checksum as a downloadable Actions artifact,
retained for 30 days. The Mac job checks both architectures and ad-hoc signs the
app. No Apple credentials or repository write permission are needed for these
test builds. This workflow does not publish releases or upload to an app store.

For distribution beyond testing, Developer ID signing and notarization can be
added later using repository secrets. Do not commit certificates or credentials.
Ad-hoc signing is not notarization and does not establish a verified publisher.

CI verifies all catalog mappings and primary rows against the existing C# app,
compiles both platforms, and runs C++ catalog/layout checks. It does not establish
that Word/Google Docs preserve focus or render marks correctly. Follow
[TESTING.md](TESTING.md) on a real Mac before calling the Mac edition verified.

On Windows, a separate interactive test can launch the native executable and a
disposable WPF editor, then verify all 54 actual buttons, input-language selection,
the non-Hebrew insertion guard, modifier handling and the global shortcut:

```powershell
dotnet build HebrewVowelKeyboard.slnx -c Release -m:1
& tests/HebrewVowelKeyboard.Tests/bin/Release/net10.0-windows/HebrewVowelKeyboard.Tests.exe --native 'C:\absolute\path\Hebrew Marks.exe'
```

Close the original palette first and leave the mouse/keyboard idle during this
test. The test fixture needs .NET; the shipped native application does not.

## Implementation

- `Source/Catalog.*`: 52 unique marks, 54 displayed buttons and RTL row definitions.
- `Source/Palette.*`: shared JUCE palette, permission button, language buttons and warning header.
- `Source/PlatformWindows.cpp`: nonactivating HWND, SendInput, focused editor layout, explicit selection request.
- `Source/PlatformMac.mm`: nonactivating NSPanel, Core Graphics Unicode events, Text Input Sources, Control-Option-H.
- `Source/Platform.h`: shared output boundary for future MIDI/keypad sources.

Selection is explicit. Mark clicks in a non-Hebrew keyboard show a warning and
insert nothing; they never change the input source or replay refused input.
The Mac app posts the key pair to the captured foreground process. Input delivery
cannot prove the editor accepted the mark or confirm the caret's location.

See [THIRD_PARTY.md](THIRD_PARTY.md) for JUCE licensing information.
