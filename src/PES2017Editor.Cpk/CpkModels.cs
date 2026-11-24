namespace PES2017Editor.Cpk;

public sealed record CpkArchive(
    string Path,
    long FileSize,
    bool HasNativeIndex,
    string? HeaderTableName,
    IReadOnlyDictionary<string, object?> HeaderValues,
    IReadOnlyList<CpkEntry> Entries,
    IReadOnlyList<string> Warnings,
    string? FailureReason)
{
    public string FileName => System.IO.Path.GetFileName(Path);
}

public sealed record CpkEntry(
    string ArchivePath,
    string InternalPath,
    long FileOffset,
    long StoredSize,
    long OriginalSize,
    int Codec,
    string? FileId,
    string? DirectoryName,
    string? FileName)
{
    public string Extension => System.IO.Path.GetExtension(InternalPath);
    public bool IsCompressed => Codec != 0 && Codec != -1;
}

public sealed record CpkChunk(string FourCc, long Offset, long Size, bool HasUtfTable, string? FailureReason);

public sealed record CpkIndexResult(IReadOnlyList<CpkArchive> Archives, IReadOnlyList<CpkEntry> Entries, IReadOnlyList<string> Warnings);
