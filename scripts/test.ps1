[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [switch]$PublicOnly
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$dotnet = Join-Path $env:ProgramFiles "dotnet\dotnet.exe"
if (-not (Test-Path -LiteralPath $dotnet) -and $env:DOTNET_ROOT) { $dotnet = Join-Path $env:DOTNET_ROOT "dotnet.exe" }
if (-not (Test-Path -LiteralPath $dotnet)) { $dotnet = (Get-Command dotnet -ErrorAction Stop).Source }
$solution = Join-Path $projectRoot "PES2017Editor.sln"

& $dotnet restore $solution --nologo
if ($LASTEXITCODE -ne 0) { throw "Restore para testes falhou." }
$testArguments = @($solution, "-c", $Configuration, "--no-restore", "--nologo", "--logger", "console;verbosity=normal")
if ($PublicOnly) { $testArguments += @("--filter", "Category!=Proprietary") }
& $dotnet test @testArguments
if ($LASTEXITCODE -ne 0) { throw "Testes falharam." }

