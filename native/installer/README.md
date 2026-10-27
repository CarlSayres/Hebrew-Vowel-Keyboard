# Windows installer and publisher signing

The GitHub Windows job builds an Inno Setup installer as well as the portable
ZIP. Until a publisher signing identity is configured, the installer is explicitly
named `HebrewMarks-<version>-Setup-unsigned.exe` and is a test artifact.

The installer uses the current user's `%LOCALAPPDATA%\Programs\Hebrew Marks`
directory, creates a Start menu shortcut and an uninstall entry, and offers an
optional desktop shortcut. It requests no administrator privileges, installs no
runtime, and does not enable automatic startup. Subsequent installs use the same
application identity and installation directory. The interactive installer asks
before closing a running installed copy when an update needs to replace it.

GitHub tests a fresh installation, reinstallation, the Start menu shortcut,
uninstall registration, rendering from the installed application, and removal.
These lifecycle tests are guarded to run only on disposable GitHub runners.

## Compile locally

With Inno Setup 6 installed and a prepared native package directory:

```powershell
./native/installer/build-windows-installer.ps1 -SourceDir artifacts/NativeHebrewMarks -OutputDir artifacts/installers
```

The default is an unsigned test installer. The script accepts `-CompilerPath`
if ISCC.exe is installed elsewhere.

## Windows signing still requires a publisher identity

Use a publicly trusted Windows code-signing certificate or signing service in
the publisher's name. No identity, certificate, private key or paid service is
created by this repository. SHA-256 download checksums are not code signatures.

`build-windows-installer.ps1 -SigningToolCommand ...` is the integration point
for an already configured signing tool. The command must contain Inno Setup's
`$f` placeholder and sign with SHA-256 plus an RFC 3161 timestamp. Use certificate
store/cloud authentication, not passwords embedded in the command. This enables
Inno Setup signing of the application, setup executable and uninstaller. The
build then rejects an invalid or untimestamped app/setup signature.

The current GitHub workflow deliberately does not pass that option. Configure
the selected provider in a separate protected release job, with signing restricted
to approved repository code. Sign the application before creating the portable
ZIP, and verify the signed installed application and uninstaller too. A release
build must fail if credentials or signature verification are missing; it must
never silently fall back to an unsigned release.

## macOS equivalent

The current Mac test build is only ad-hoc signed. Distribution signing uses a
**Developer ID Application** certificate from an Apple Developer Program account,
with hardened runtime and a secure timestamp. Submit the signed app/archive using
Apple's `notarytool`, require an Accepted result, staple the notarization ticket
to the app, then package the stapled app and verify Gatekeeper acceptance.

A drag-to-Applications DMG can contain this app. A `.pkg` installer is optional;
if used, it additionally needs a **Developer ID Installer** certificate. Signing
and notarization can run on the existing GitHub macOS runner with credentials in
protected secrets and an ephemeral keychain. These steps have not been enabled:
Apple account/certificate setup is still required. Users must still grant the
Accessibility permission needed for text insertion.

References:
- https://learn.microsoft.com/en-us/windows/win32/seccrypto/signtool
- https://jrsoftware.org/ishelp/topic_setup_signtool.htm
- https://developer.apple.com/developer-id/
- https://developer.apple.com/documentation/security/notarizing-macos-software-before-distribution
