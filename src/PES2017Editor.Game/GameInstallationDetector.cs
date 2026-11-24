using System.Diagnostics;
using PES2017Editor.Core;

namespace PES2017Editor.Game;

public sealed class GameInstallationDetector
{
    private static readonly string[] SaveNames = ["EDIT00000000", "EDIT00000001", "EDIT00000002"];

    public Task<InstallationInfo> DetectAsync(string selectedPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = Path.GetFullPath(selectedPath.Trim().Trim('"'));
        var executable = FindFile(root, "PES2017.exe");
        var data = Path.Combine(root, "Data");
        var download = Path.Combine(root, "download");
        var cpks = new List<string>();
        AddCpks(data, cpks);
        AddCpks(download, cpks);
        var warnings = new List<string>();
        if (executable is null) warnings.Add("PES2017.exe não encontrado na pasta selecionada.");
        if (!Directory.Exists(data)) warnings.Add("Pasta Data não encontrada.");
        if (cpks.Count == 0) warnings.Add("Nenhum CPK encontrado em Data ou download.");

        string? version = null;
        if (executable is not null)
        {
            try
            {
                version = FileVersionInfo.GetVersionInfo(executable).FileVersion;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                warnings.Add($"Versão do executável não pôde ser lida: {ex.Message}");
            }
        }

        var dataPackCandidates = Directory.Exists(download)
            ? Directory.EnumerateFiles(download, "*", SearchOption.TopDirectoryOnly)
                .Where(IsDataPackCandidate).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray()
            : Array.Empty<string>();
        var patchCandidates = Directory.Exists(root)
            ? Directory.EnumerateDirectories(root, "*", SearchOption.TopDirectoryOnly)
                .Where(x => Path.GetFileName(x).Contains("patch", StringComparison.OrdinalIgnoreCase) ||
                            Path.GetFileName(x).Contains("sider", StringComparison.OrdinalIgnoreCase))
                .ToArray()
            : Array.Empty<string>();
        var saveCandidates = Directory.Exists(root)
            ? Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Where(x => SaveNames.Any(n => Path.GetFileName(x).Equals(n, StringComparison.OrdinalIgnoreCase)) ||
                            Path.GetFileName(x).StartsWith("EDIT", StringComparison.OrdinalIgnoreCase))
                .Take(100).ToArray()
            : Array.Empty<string>();

        return Task.FromResult(new InstallationInfo(root, executable, version, Directory.Exists(data),
            Directory.Exists(download), File.Exists(Path.Combine(data, "DpFileList.bin")), dataPackCandidates,
            patchCandidates, saveCandidates, cpks, warnings));
    }

    public static bool LooksLikeInstallation(string path)
    {
        var full = Path.GetFullPath(path);
        return File.Exists(Path.Combine(full, "PES2017.exe")) ||
               Directory.Exists(Path.Combine(full, "Data"));
    }

    private static string? FindFile(string root, string name)
    {
        var direct = Path.Combine(root, name);
        if (File.Exists(direct)) return direct;
        try
        {
            return Directory.EnumerateFiles(root, name, SearchOption.TopDirectoryOnly).FirstOrDefault();
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static void AddCpks(string directory, List<string> destination)
    {
        if (!Directory.Exists(directory)) return;
        try
        {
            destination.AddRange(Directory.EnumerateFiles(directory, "*.cpk", SearchOption.TopDirectoryOnly)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
        }
        catch (UnauthorizedAccessException)
        {
            // The caller will see the missing files through the warning generated above.
        }
    }

    private static bool IsDataPackCandidate(string path)
    {
        var name = Path.GetFileName(path);
        return name.Contains("dt", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("dp", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("Data", StringComparison.OrdinalIgnoreCase);
    }
}
