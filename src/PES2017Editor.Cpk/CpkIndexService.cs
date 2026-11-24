using PES2017Editor.Core;

namespace PES2017Editor.Cpk;

public sealed class CpkIndexService
{
    private readonly ICpkArchiveBackend _reader;

    public CpkIndexService(ICpkArchiveBackend? reader = null) => _reader = reader ?? new CpkArchiveReader();

    public async Task<CpkIndexResult> IndexAsync(
        InstallationInfo installation,
        IProgress<ProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var archives = new List<CpkArchive>();
        var allEntries = new List<CpkEntry>();
        var warnings = new List<string>();
        var paths = installation.CpkPaths.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        for (var index = 0; index < paths.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = paths[index];
            progress?.Report(new ProgressUpdate("Indexando arquivos CPK", index, paths.Length, Path.GetFileName(path)));
            var archive = await _reader.ReadAsync(path, cancellationToken);
            archives.Add(archive);
            allEntries.AddRange(archive.Entries);
            warnings.AddRange(archive.Warnings.Select(w => $"{Path.GetFileName(path)}: {w}"));
        }

        progress?.Report(new ProgressUpdate("Indexando arquivos CPK", paths.Length, paths.Length));
        var duplicateGroups = allEntries.GroupBy(e => e.InternalPath, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Select(e => e.ArchivePath).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
            .ToArray();
        warnings.AddRange(duplicateGroups.Select(g => $"Arquivo duplicado em {g.Count()} CPKs: {g.Key}"));
        return new CpkIndexResult(archives, allEntries, warnings);
    }
}
