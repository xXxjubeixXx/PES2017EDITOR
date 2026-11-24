namespace PES2017Editor.Core;

public sealed record InstallationInfo(
    string RootPath,
    string? ExecutablePath,
    string? ExecutableVersion,
    bool HasDataDirectory,
    bool HasDownloadDirectory,
    bool HasDpFileList,
    IReadOnlyList<string> DataPackCandidates,
    IReadOnlyList<string> PatchCandidates,
    IReadOnlyList<string> SaveCandidates,
    IReadOnlyList<string> CpkPaths,
    IReadOnlyList<string> Warnings);

public sealed record ProgressUpdate(string Stage, int Completed, int Total, string? Detail = null);

public enum SupportStatus
{
    Supported,
    PartiallySupported,
    NotSupported,
    Invalid
}

public sealed record IntegrityIssue(string Code, string Message, SupportStatus Status = SupportStatus.PartiallySupported);

public sealed record DashboardFacts(
    int CpkCount,
    int IndexedFileCount,
    string? ExecutableVersion,
    long TotalCpkBytes,
    int ErrorCount,
    string? DataPack,
    SupportStatus Compatibility,
    IReadOnlyList<IntegrityIssue> Issues);
