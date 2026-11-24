[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$ExecutablePath,
    [string]$WorkingDirectory,
    [string]$Arguments = "",
    [int]$TimeoutSeconds = 10,
    [switch]$KeepOpen
)

$ErrorActionPreference = "Stop"
$resolvedExecutable = (Resolve-Path -LiteralPath $ExecutablePath).Path
if ([string]::IsNullOrWhiteSpace($WorkingDirectory)) { $WorkingDirectory = Split-Path -Parent $resolvedExecutable }
if (-not (Test-Path -LiteralPath $WorkingDirectory -PathType Container)) { throw "Diretório de trabalho não existe: $WorkingDirectory" }

if (-not ('PES2017Editor.StartupWindowProbe' -as [type])) {
    Add-Type @"
using System;
using System.Runtime.InteropServices;
namespace PES2017Editor {
    public static class StartupWindowProbe {
        [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left; public int Top; public int Right; public int Bottom; }
        [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr handle);
        [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr handle);
        [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr handle);
        [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr handle, out Rect rect);
    }
}
"@
}

$start = Get-Date
$startParameters = @{ FilePath = $resolvedExecutable; WorkingDirectory = $WorkingDirectory; PassThru = $true }
if (-not [string]::IsNullOrWhiteSpace($Arguments)) { $startParameters.ArgumentList = $Arguments }
$process = Start-Process @startParameters
try {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $handle = [IntPtr]::Zero
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 250
        $process.Refresh()
        if ($process.HasExited) {
            throw "O processo encerrou antes de criar a janela. ExitCode=$($process.ExitCode); executável=$resolvedExecutable"
        }
        $handle = $process.MainWindowHandle
        if ($handle -ne [IntPtr]::Zero -and [PES2017Editor.StartupWindowProbe]::IsWindow($handle)) { break }
    }

    if ($handle -eq [IntPtr]::Zero -or -not [PES2017Editor.StartupWindowProbe]::IsWindow($handle)) {
        throw "O processo permaneceu ativo, mas não criou uma janela principal visível dentro de ${TimeoutSeconds}s. PID=$($process.Id)"
    }

    $rect = New-Object PES2017Editor.StartupWindowProbe+Rect
    if (-not [PES2017Editor.StartupWindowProbe]::GetWindowRect($handle, [ref]$rect)) { throw "Não foi possível obter o retângulo da janela." }
    $width = $rect.Right - $rect.Left
    $height = $rect.Bottom - $rect.Top
    $visible = [PES2017Editor.StartupWindowProbe]::IsWindowVisible($handle)
    $minimized = [PES2017Editor.StartupWindowProbe]::IsIconic($handle)
    $process.Refresh()
    $result = [PSCustomObject]@{
        Executable = $resolvedExecutable
        Pid = $process.Id
        MainWindowHandle = $handle.ToInt64()
        MainWindowTitle = $process.MainWindowTitle
        Visible = $visible
        Minimized = $minimized
        Responding = $process.Responding
        Left = $rect.Left
        Top = $rect.Top
        Width = $width
        Height = $height
        StartedAt = $start.ToString("o")
    }
    $result | ConvertTo-Json -Compress
    if (-not $visible -or $minimized -or $width -le 0 -or $height -le 0 -or -not $process.Responding) { throw "A janela foi criada, mas não passou na validação de visibilidade/responsividade." }
}
finally {
    if (-not $KeepOpen -and $process -and -not $process.HasExited) {
        if (-not $process.CloseMainWindow()) { $process.Kill() }
        $process.WaitForExit(3000)
        if (-not $process.HasExited) { $process.Kill() }
    }
}
