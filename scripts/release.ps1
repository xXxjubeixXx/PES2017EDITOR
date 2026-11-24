[CmdletBinding()]
param(
    [switch]$PrepareOnly,
    [switch]$Publish,
    [switch]$SkipTests,
    [switch]$PublicOnly
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$props = Get-Content -Raw (Join-Path $projectRoot "Directory.Build.props")
$version = [regex]::Match($props, '<VersionPrefix>([^<]+)</VersionPrefix>').Groups[1].Value
if ([string]::IsNullOrWhiteSpace($version)) { throw "VersionPrefix não encontrado." }
$tag = "v$version"
if ($env:GITHUB_EVENT_NAME -eq "push" -and $env:GITHUB_REF_NAME -and $env:GITHUB_REF_NAME -ne $tag) { throw "A tag $env:GITHUB_REF_NAME não corresponde à versão centralizada $tag." }
$releaseRoot = Join-Path $projectRoot "artifacts/release/$version"
$installer = Join-Path $projectRoot "artifacts/installer/PES2017DatabaseEditor-Setup-v$version-win-x64.exe"
$portable = Join-Path $releaseRoot "PES2017DatabaseEditor-Portable-v$version-win-x64.zip"
$publishRoot = Join-Path $projectRoot "artifacts/installer-payload"

if (-not $SkipTests) {
    $testArguments = @{}
    if ($PublicOnly) { $testArguments.PublicOnly = $true }
    & (Join-Path $PSScriptRoot "test.ps1") @testArguments
}
& (Join-Path $PSScriptRoot "publish.ps1") -OutputDirectory "artifacts/installer-payload"
& (Join-Path $PSScriptRoot "build-installer.ps1") -SkipPublish
if (Test-Path -LiteralPath $releaseRoot) { Remove-Item -LiteralPath $releaseRoot -Recurse -Force }
New-Item -ItemType Directory -Path $releaseRoot -Force | Out-Null
Compress-Archive -Path (Join-Path $publishRoot "*") -DestinationPath $portable -CompressionLevel Optimal

$artifactFiles = @($installer, $portable)
$hashLines = foreach ($file in $artifactFiles) {
    $hash = (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $([IO.Path]::GetFileName($file))"
}
$hashFile = Join-Path $releaseRoot "SHA256SUMS.txt"
$hashLines | Set-Content -LiteralPath $hashFile -Encoding utf8
$installerHash = ($hashLines | Where-Object { $_ -like "*$(Split-Path $installer -Leaf)" }).Split(' ')[0]
$portableHash = ($hashLines | Where-Object { $_ -like "*$(Split-Path $portable -Leaf)" }).Split(' ')[0]
$installerSize = (Get-Item -LiteralPath $installer).Length
$portableSize = (Get-Item -LiteralPath $portable).Length
$manifest = [ordered]@{
    version = $version
    publishedAt = (Get-Date).ToUniversalTime().ToString("o")
    channel = "stable"
    architecture = "win-x64"
    installer = [ordered]@{
        name = (Split-Path $installer -Leaf)
        url = "https://github.com/xXxjubeixXx/PES2017EDITOR/releases/download/$tag/$(Split-Path $installer -Leaf)"
        size = $installerSize
        sha256 = $installerHash
    }
    portable = [ordered]@{
        name = (Split-Path $portable -Leaf)
        url = "https://github.com/xXxjubeixXx/PES2017EDITOR/releases/download/$tag/$(Split-Path $portable -Leaf)"
        size = $portableSize
        sha256 = $portableHash
    }
    releaseUrl = "https://github.com/xXxjubeixXx/PES2017EDITOR/releases/tag/$tag"
    changelogUrl = "https://github.com/xXxjubeixXx/PES2017EDITOR/blob/$tag/docs/CHANGELOG.md"
}
$manifestPath = Join-Path $releaseRoot "update-manifest.json"
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestPath -Encoding utf8

if ($PrepareOnly) {
    Write-Host "Artefatos preparados em $releaseRoot"
    exit 0
}

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) { throw "GitHub CLI não encontrado." }
gh auth status
if ($LASTEXITCODE -ne 0) { throw "Autenticação GitHub indisponível." }
if ($Publish) {
    gh release create $tag $installer $portable $hashFile $manifestPath --repo xXxjubeixXx/PES2017EDITOR `
        --title "PES 2017 Database Editor $tag" --notes-file (Join-Path $projectRoot "docs/PHASE_02_REPORT.md")
    if ($LASTEXITCODE -ne 0) { throw "Publicação da release falhou." }
}

Write-Host "Release local preparada para $tag"
