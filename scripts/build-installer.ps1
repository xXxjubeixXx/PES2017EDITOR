[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [switch]$SkipPublish,
    [string]$IsccPath = ""
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$versionLine = Select-String -LiteralPath (Join-Path $projectRoot "Directory.Build.props") -Pattern '<VersionPrefix>([^<]+)</VersionPrefix>' | Select-Object -First 1
if ($null -eq $versionLine) { throw "VersionPrefix não encontrado." }
$version = [regex]::Match($versionLine.Line, '<VersionPrefix>([^<]+)</VersionPrefix>').Groups[1].Value
$payload = Join-Path $projectRoot "artifacts/installer-payload"
$output = Join-Path $projectRoot "artifacts/installer"

if (-not $SkipPublish) { & (Join-Path $PSScriptRoot "publish.ps1") -Configuration $Configuration -OutputDirectory "artifacts/installer-payload" }
if (-not (Test-Path -LiteralPath $payload)) { throw "Payload não encontrado: $payload" }
New-Item -ItemType Directory -Path $output -Force | Out-Null

if ([string]::IsNullOrWhiteSpace($IsccPath)) {
    $candidates = @(
        (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"),
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe"
    )
    $IsccPath = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if ([string]::IsNullOrWhiteSpace($IsccPath) -or -not (Test-Path -LiteralPath $IsccPath)) { throw "ISCC.exe do Inno Setup não encontrado." }

$iss = Join-Path $projectRoot "installer/PES2017DatabaseEditor.iss"
& $IsccPath "/DAppVersion=$version" "/DSourceDir=$payload" "/DOutputDir=$output" $iss
if ($LASTEXITCODE -ne 0) { throw "Compilação do instalador falhou." }

$installer = Join-Path $output "PES2017DatabaseEditor-Setup-v$version-win-x64.exe"
if (-not (Test-Path -LiteralPath $installer)) { throw "Instalador não foi gerado." }
Write-Host "Instalador concluído: $installer"
