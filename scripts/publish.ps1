[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$OutputDirectory = "artifacts/publish/win-x64"
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$dotnet = Join-Path $env:ProgramFiles "dotnet\dotnet.exe"
if (-not (Test-Path -LiteralPath $dotnet) -and $env:DOTNET_ROOT) { $dotnet = Join-Path $env:DOTNET_ROOT "dotnet.exe" }
if (-not (Test-Path -LiteralPath $dotnet)) { $dotnet = (Get-Command dotnet -ErrorAction Stop).Source }
$app = Join-Path $projectRoot "src/PES2017Editor.App/PES2017Editor.App.csproj"
$updater = Join-Path $projectRoot "src/PES2017Editor.Updater/PES2017Editor.Updater.csproj"
$output = [IO.Path]::GetFullPath((Join-Path $projectRoot $OutputDirectory))
$buildDate = Get-Date -Format "yyyy-MM-ddTHH:mm:sszzz"

& $dotnet restore $app -r win-x64 --nologo
if ($LASTEXITCODE -ne 0) { throw "Restore de publicação falhou." }
if (Test-Path -LiteralPath $output) { Remove-Item -LiteralPath $output -Recurse -Force }
New-Item -ItemType Directory -Path $output -Force | Out-Null

& $dotnet publish $app -c $Configuration -r win-x64 --self-contained true --no-restore `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None `
    -p:BuildDate=$buildDate -o $output
if ($LASTEXITCODE -ne 0) { throw "Publicação do aplicativo falhou." }

& $dotnet publish $updater -c $Configuration -r win-x64 --self-contained true --no-restore `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None `
    -p:BuildDate=$buildDate -o $output
if ($LASTEXITCODE -ne 0) { throw "Publicação do atualizador falhou." }

Copy-Item (Join-Path $projectRoot "README.md") (Join-Path $output "README.md") -Force
Write-Host "Publicação concluída: $output"
