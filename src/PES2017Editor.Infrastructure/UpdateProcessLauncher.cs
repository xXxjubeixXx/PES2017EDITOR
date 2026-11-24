using System.Diagnostics;

namespace PES2017Editor.Infrastructure;

public static class UpdateProcessLauncher
{
    public static bool TryLaunch(string installerPath)
    {
        var helperPath = Path.Combine(AppContext.BaseDirectory, "PES2017Editor.Updater.exe");
        if (!File.Exists(helperPath) || !File.Exists(installerPath)) return false;
        var process = Process.GetCurrentProcess();
        Process.Start(new ProcessStartInfo
        {
            FileName = helperPath,
            WorkingDirectory = AppContext.BaseDirectory,
            UseShellExecute = true,
            ArgumentList =
            {
                "--pid", process.Id.ToString(),
                "--installer", Path.GetFullPath(installerPath)
            }
        });
        return true;
    }
}
