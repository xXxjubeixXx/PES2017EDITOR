namespace PES2017Editor.Cpk;

public sealed record ExperimentalCpkResult(string OutputPath, string SourcePath, string InternalPath,
    long OriginalStoredSize, long ReplacementStoredSize, bool LayoutPreserved, IReadOnlyList<string> Warnings);

/// <summary>
/// Creates a safe, layout-preserving experimental copy. It never writes to the source CPK.
/// A full TOC rebuild is intentionally not claimed here; replacements must fit the original slot.
/// </summary>
public sealed class ExperimentalCpkWriter
{
    public async Task<ExperimentalCpkResult> CreateLayoutPreservingCopyAsync(CpkEntry entry,
        ReadOnlyMemory<byte> replacementStoredBytes, string outputPath,
        CancellationToken cancellationToken = default)
    {
        if (replacementStoredBytes.Length > entry.StoredSize)
            throw new InvalidDataException($"A nova entrada ({replacementStoredBytes.Length:N0}) excede o slot original ({entry.StoredSize:N0}).");
        var source = Path.GetFullPath(entry.ArchivePath);
        var destination = Path.GetFullPath(outputPath);
        if (string.Equals(source, destination, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("A cópia experimental não pode ser o arquivo original.");
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.Copy(source, destination, overwrite: false);
        try
        {
            await using var stream = new FileStream(destination, FileMode.Open, FileAccess.Write, FileShare.Read,
                128 * 1024, FileOptions.Asynchronous | FileOptions.RandomAccess);
            stream.Position = entry.FileOffset;
            await stream.WriteAsync(replacementStoredBytes, cancellationToken);
            var remaining = entry.StoredSize - replacementStoredBytes.Length;
            var zeros = new byte[Math.Min(128 * 1024, Math.Max(1, (int)Math.Min(remaining, int.MaxValue)))];
            while (remaining > 0)
            {
                var count = (int)Math.Min(zeros.Length, remaining);
                await stream.WriteAsync(zeros.AsMemory(0, count), cancellationToken);
                remaining -= count;
            }
            await stream.FlushAsync(cancellationToken);
        }
        catch
        {
            // Keep the failed artifact for diagnosis, but make the failure explicit to the caller.
            throw;
        }
        return new ExperimentalCpkResult(destination, source, entry.InternalPath, entry.StoredSize,
            replacementStoredBytes.Length, true,
            ["A cópia foi gerada preservando a TOC e o tamanho do slot; reconstrução geral de CPK ainda não está habilitada."]);
    }
}
