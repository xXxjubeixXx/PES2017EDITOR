using System.Diagnostics;

if (!TryGetArgument("--installer", out var installer) || !TryGetArgument("--pid", out var pidText) ||
    !int.TryParse(pidText, out var parentPid) || !Path.IsPathFullyQualified(installer) ||
    !installer.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || !File.Exists(installer))
{
    return 2;
}

try
{
    try
    {
        using var parent = Process.GetProcessById(parentPid);
        if (!parent.HasExited) await parent.WaitForExitAsync().WaitAsync(TimeSpan.FromMinutes(10));
    }
    catch (ArgumentException)
    {
        // The parent exited before the helper could attach.
    }

    Process.Start(new ProcessStartInfo
    {
        FileName = installer,
        WorkingDirectory = Path.GetDirectoryName(installer)!,
        UseShellExecute = true
    });
    return 0;
}
catch
{
    return 1;
}

static bool TryGetArgument(string name, out string value)
{
    value = string.Empty;
    var index = Array.FindIndex(Environment.GetCommandLineArgs(), argument =>
        argument.Equals(name, StringComparison.OrdinalIgnoreCase));
    if (index < 0 || index + 1 >= Environment.GetCommandLineArgs().Length) return false;
    value = Environment.GetCommandLineArgs()[index + 1];
    return !string.IsNullOrWhiteSpace(value);
}
