param([switch]$Integration)
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
$env:DOTNET_CLI_HOME = Join-Path $PSScriptRoot '.dotnet'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'

dotnet build HebrewVowelKeyboard.slnx -c Release -m:1 -p:UseSharedCompilation=false -p:NuGetAudit=false --ignore-failed-sources
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

$testExecutable = Join-Path $PSScriptRoot 'tests\HebrewVowelKeyboard.Tests\bin\Release\net10.0-windows\HebrewVowelKeyboard.Tests.exe'
& $testExecutable
if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
if ($Integration) {
    & $testExecutable --integration
    if ($LASTEXITCODE -ne 0) { throw 'Interactive tests failed.' }
}

dotnet publish src\HebrewVowelKeyboard\HebrewVowelKeyboard.csproj -c Release --no-restore --self-contained false -o artifacts\HebrewMarks
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
Write-Host 'Ready: artifacts\HebrewMarks\HebrewVowelKeyboard.exe'
