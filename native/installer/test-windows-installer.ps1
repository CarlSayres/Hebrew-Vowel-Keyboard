param([Parameter(Mandatory)][string]$Installer)
$ErrorActionPreference = 'Stop'
# This test writes an installation and HKCU uninstall/Start menu entries. Run
# only on a disposable GitHub runner, never against a user's installed copy.
if ($env:GITHUB_ACTIONS -ne 'true' -or -not $env:RUNNER_TEMP) {
    throw 'Installer lifecycle tests must run on a disposable GitHub Actions runner.'
}
$installerPath = (Resolve-Path -LiteralPath $Installer).Path
$installPath = Join-Path $env:RUNNER_TEMP 'HebrewMarks-Installer-Test'
$registryPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{78AC0E49-2F0A-4AE5-9F21-5FED1921074E}_is1'
$shortcut = Join-Path ([Environment]::GetFolderPath('Programs')) 'Hebrew Marks\Hebrew Marks.lnk'
if ((Test-Path -LiteralPath $registryPath) -or (Test-Path -LiteralPath $installPath) -or (Test-Path -LiteralPath $shortcut)) {
    throw 'Existing installation detected; refusing to change it in a test.'
}
try {
    foreach ($pass in 1..2) {
        $args = @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/SP-', '/CURRENTUSER',
            "/DIR=`"$installPath`"", "/LOG=`"$(Join-Path $env:RUNNER_TEMP "installer-$pass.log")`"")
        $process = Start-Process -FilePath $installerPath -ArgumentList $args -WindowStyle Hidden -PassThru -Wait
        if ($process.ExitCode -ne 0) { throw "Install pass $pass failed ($($process.ExitCode))." }
        foreach ($name in @('Hebrew Marks.exe', 'unins000.exe', 'JUCE-LICENSE.md')) {
            if (-not (Test-Path -LiteralPath (Join-Path $installPath $name))) { throw "Installed file missing: $name" }
        }
        if (-not (Test-Path -LiteralPath $registryPath)) { throw 'Uninstall registration missing' }
        if (-not (Test-Path -LiteralPath $shortcut)) { throw 'Start menu shortcut missing' }
    }
    $previewPath = Join-Path $env:RUNNER_TEMP 'installed-previews'
    $preview = Start-Process -FilePath (Join-Path $installPath 'Hebrew Marks.exe') -ArgumentList "--render `"$previewPath`"" -WindowStyle Hidden -PassThru -Wait
    if ($preview.ExitCode -ne 0 -or -not (Test-Path -LiteralPath (Join-Path $previewPath 'niqqud.png'))) {
        throw 'Installed application did not render successfully'
    }
    Write-Output 'PASS fresh install, reinstall, Start menu shortcut, uninstall registration and installed-app rendering'
}
finally {
    $uninstaller = Join-Path $installPath 'unins000.exe'
    if (Test-Path -LiteralPath $uninstaller) {
        $process = Start-Process -FilePath $uninstaller -ArgumentList '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART' -WindowStyle Hidden -PassThru -Wait
        if ($process.ExitCode -ne 0) { throw "Uninstall failed ($($process.ExitCode))." }
    }
}
if ((Test-Path -LiteralPath (Join-Path $installPath 'Hebrew Marks.exe')) -or
    (Test-Path -LiteralPath $registryPath) -or (Test-Path -LiteralPath $shortcut)) {
    throw 'Uninstall left the application, registration or shortcut behind'
}
Write-Output 'PASS uninstall removes installed app, registration and Start menu shortcut'
