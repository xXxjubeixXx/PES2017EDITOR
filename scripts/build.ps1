[CmdletBinding()]
param([string]$Configuration = "Release")

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$dotnet = Join-Path $env:ProgramFiles "dotnet\dotnet.exe"
if (-not (Test-Path -LiteralPath $dotnet) -and $env:DOTNET_ROOT) { $dotnet = Join-Path $env:DOTNET_ROOT "dotnet.exe" }
if (-not (Test-Path -LiteralPath $dotnet)) { $dotnet = (Get-Command dotnet -ErrorAction Stop).Source }
$solution = Join-Path $projectRoot "PES2017Editor.sln"
$buildDate = Get-Date -Format "yyyy-MM-ddTHH:mm:sszzz"

& $dotnet restore $solution --nologo
if ($LASTEXITCODE -ne 0) { throw "Restore falhou." }
& $dotnet build $solution -c $Configuration --no-restore --nologo -p:BuildDate=$buildDate
if ($LASTEXITCODE -ne 0) { throw "Build falhou." }

Write-Host "Build concluído: $Configuration"
