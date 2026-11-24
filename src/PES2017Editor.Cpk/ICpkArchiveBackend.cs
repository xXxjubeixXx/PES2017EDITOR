namespace PES2017Editor.Cpk;

/// <summary>
/// Boundary used by the editor core to access CPKs. Implementations may be
/// native, managed, or an isolated x86 helper without leaking that choice into
/// the UI or binary parsers.
/// </summary>
public interface ICpkArchiveBackend
{
    Task<CpkArchive> ReadAsync(string path, CancellationToken cancellationToken = default);
    Task<byte[]> ReadEntryAsync(CpkEntry entry, CancellationToken cancellationToken = default);
    Task<byte[]> ReadEntryPreviewAsync(CpkEntry entry, int maxBytes = 256, CancellationToken cancellationToken = default);
    Task ExtractEntryAsync(CpkEntry entry, string destinationPath, CancellationToken cancellationToken = default);
}
