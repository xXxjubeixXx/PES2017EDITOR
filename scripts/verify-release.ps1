[CmdletBinding()]
param([string]$Repository = "xXxjubeixXx/PES2017EDITOR")

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$props = Get-Content -Raw (Join-Path $projectRoot "Directory.Build.props")
$version = [regex]::Match($props, '<VersionPrefix>([^<]+)</VersionPrefix>').Groups[1].Value
$release = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repository/releases/latest" -Headers @{ 'User-Agent' = 'PES2017DatabaseEditor-ReleaseVerifier' }
if ($release.tag_name -ne "v$version") { throw "Latest release $($release.tag_name) não corresponde a v$version." }
if ($release.draft -or $release.prerelease) { throw "A latest release não é estável." }

$required = @(
    "PES2017DatabaseEditor-Setup-v$version-win-x64.exe",
    "PES2017DatabaseEditor-Portable-v$version-win-x64.zip",
    "SHA256SUMS.txt",
    "update-manifest.json"
)
foreach ($name in $required) {
    if (-not ($release.assets | Where-Object name -eq $name)) { throw "Asset ausente: $name" }
}
$manifestAsset = $release.assets | Where-Object name -eq "update-manifest.json"
$manifest = Invoke-RestMethod -Uri $manifestAsset.browser_download_url -Headers @{ 'User-Agent' = 'PES2017DatabaseEditor-ReleaseVerifier' }
if ($manifest.version -ne $version -or $manifest.channel -ne "stable" -or $manifest.architecture -ne "win-x64") {
    throw "Manifesto incompatível com a release."
}
$installerAsset = $release.assets | Where-Object name -eq $manifest.installer.name
if ($null -eq $installerAsset -or [int64]$installerAsset.size -ne [int64]$manifest.installer.size) {
    throw "Tamanho do instalador diverge entre API e manifesto."
}

Write-Host "Release verificada: $($release.html_url)"
Write-Host "Installer: $($manifest.installer.name) / $($manifest.installer.size) bytes"
Write-Host "Portable: $($manifest.portable.name) / $($manifest.portable.size) bytes"
Write-Host "SHA-256 no manifesto: $($manifest.installer.sha256)"
