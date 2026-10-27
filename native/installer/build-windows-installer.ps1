param(
    [Parameter(Mandatory)][string]$SourceDir,
    [Parameter(Mandatory)][string]$OutputDir,
    [string]$CompilerPath,
    # Trusted operator command, using $f for the quoted file to sign. Use a
    # certificate store/cloud authentication; never put a password in this string.
    [string]$SigningToolCommand
)
$ErrorActionPreference = 'Stop'
$sourcePath = (Resolve-Path -LiteralPath $SourceDir).Path
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
$outputPath = (Resolve-Path -LiteralPath $OutputDir).Path
foreach ($name in @('Hebrew Marks.exe', 'README.md', 'THIRD_PARTY.md', 'JUCE-LICENSE.md', 'JUCE.spdx.json')) {
    if (-not (Test-Path -LiteralPath (Join-Path $sourcePath $name) -PathType Leaf)) { throw "Missing package file: $name" }
}
if (-not $CompilerPath) {
    $compilerCommand = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($compilerCommand) { $CompilerPath = $compilerCommand.Source }
    else {
        $CompilerPath = Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'
    }
}
if (-not (Test-Path -LiteralPath $CompilerPath -PathType Leaf)) {
    throw 'Inno Setup 6 is required. Install it or supply -CompilerPath. GitHub Windows runners already include it.'
}
$version = (Get-Item -LiteralPath (Join-Path $sourcePath 'Hebrew Marks.exe')).VersionInfo.FileVersion
if ($version -notmatch '^\d+\.\d+\.\d+(\.\d+)?$') { throw "Invalid application version: $version" }
$compilerArgs = @('/Qp', "/DSourceDir=$sourcePath", "/DOutputDir=$outputPath", "/DAppVersion=$version")
if ($SigningToolCommand) {
    if (-not $SigningToolCommand.Contains('$f')) { throw 'SigningToolCommand must include the Inno Setup $f file placeholder.' }
    $compilerArgs += @('/DSignedBuild', "/Spublisher=$SigningToolCommand")
}
$compilerArgs += (Join-Path $PSScriptRoot 'windows.iss')
& $CompilerPath @compilerArgs
if ($LASTEXITCODE -ne 0) { throw "Installer compilation failed ($LASTEXITCODE)." }
$suffix = if ($SigningToolCommand) { '' } else { '-unsigned' }
$installer = Join-Path $outputPath "HebrewMarks-$version-Setup$suffix.exe"
if (-not (Test-Path -LiteralPath $installer)) { throw "Expected installer missing: $installer" }
if ($SigningToolCommand) {
    foreach ($file in @($installer, (Join-Path $sourcePath 'Hebrew Marks.exe'))) {
        $signature = Get-AuthenticodeSignature -LiteralPath $file
        if ($signature.Status -ne 'Valid' -or -not $signature.TimeStamperCertificate) {
            throw "A valid, timestamped publisher signature is required: $file"
        }
    }
}
$hash = (Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $([IO.Path]::GetFileName($installer))" | Set-Content -LiteralPath "$installer.sha256.txt" -Encoding utf8
Write-Output $installer
