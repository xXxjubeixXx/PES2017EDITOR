using System.Buffers.Binary;
using System.Text;
using PES2017Editor.Binary;
using PES2017Editor.Cpk;

namespace PES2017Editor.Database;

public enum EvidenceLevel
{
    Proven,
    PartiallyProven,
    Hypothesis,
    Unknown
}

public sealed record PesPlayerRecord(
    int Index,
    uint Id,
    string Name,
    string ShirtName,
    int RecordOffset,
    byte[] RawRecord,
    EvidenceLevel NameEvidence = EvidenceLevel.PartiallyProven)
{
    public const int RecordSize = 192;
    public const int IdOffset = 8;
    public const int ShirtNameOffset = 98;
    public const int ShirtNameCapacity = 46;
    public const int NameOffset = 144;
    public const int NameCapacity = 48;
}

public sealed record PesTeamRecord(
    int Index,
    uint Id,
    string Name,
    int RecordOffset,
    byte[] RawRecord,
    EvidenceLevel NameEvidence = EvidenceLevel.PartiallyProven)
{
    public const int RecordSize = 1400;
    public const int IdOffset = 8;
    public const int NameOffset = 234;
    public const int NameCapacity = 70;
}

public sealed record PesPlayerAssignmentRecord(
    int Index,
    uint PlayerIdCandidate,
    uint TeamIdCandidate,
    byte ShirtNumber,
    byte OrderAndFlags,
    byte Flags,
    byte[] RawRecord)
{
    public const int RecordSize = 16;
}

public sealed record PesCompetitionRecord(
    int Index,
    string Name,
    int RecordOffset,
    byte[] RawRecord,
    EvidenceLevel Evidence = EvidenceLevel.PartiallyProven)
{
    public const int RecordSize = 32;
    public const int NameOffset = 4;
    public const int NameCapacity = 28;
}

public sealed record PesBinaryDocument(
    CpkEntry Entry,
    byte[] OriginalStoredBytes,
    WesysDocument Wesys,
    byte[] OriginalPayload);

public sealed record PesDatabaseSnapshot(
    IReadOnlyList<PesPlayerRecord> Players,
    IReadOnlyList<PesTeamRecord> Teams,
    IReadOnlyList<PesCompetitionRecord> Competitions,
    IReadOnlyList<PesPlayerAssignmentRecord> Assignments,
    IReadOnlyDictionary<uint, uint> PlayerToTeam,
    PesBinaryDocument? PlayerDocument,
    PesBinaryDocument? TeamDocument,
    PesBinaryDocument? CompetitionDocument,
    PesBinaryDocument? AssignmentDocument,
    IReadOnlyList<string> Warnings);

public static class PesDatabaseParser
{
    public static IReadOnlyList<PesPlayerRecord> ParsePlayers(ReadOnlySpan<byte> payload)
    {
        var count = payload.Length / PesPlayerRecord.RecordSize;
        var result = new List<PesPlayerRecord>(count);
        for (var i = 0; i < count; i++)
        {
            var offset = i * PesPlayerRecord.RecordSize;
            var record = payload.Slice(offset, PesPlayerRecord.RecordSize).ToArray();
            result.Add(new PesPlayerRecord(i, BinaryPrimitives.ReadUInt32LittleEndian(record.AsSpan(8, 4)),
                ReadFixedUtf8(record, PesPlayerRecord.NameOffset, PesPlayerRecord.NameCapacity),
                ReadFixedUtf8(record, PesPlayerRecord.ShirtNameOffset, PesPlayerRecord.ShirtNameCapacity),
                offset, record));
        }
        return result;
    }

    public static IReadOnlyList<PesTeamRecord> ParseTeams(ReadOnlySpan<byte> payload)
    {
        var count = payload.Length / PesTeamRecord.RecordSize;
        var result = new List<PesTeamRecord>(count);
        for (var i = 0; i < count; i++)
        {
            var offset = i * PesTeamRecord.RecordSize;
            var record = payload.Slice(offset, PesTeamRecord.RecordSize).ToArray();
            result.Add(new PesTeamRecord(i, BinaryPrimitives.ReadUInt32LittleEndian(record.AsSpan(8, 4)),
                ReadFixedUtf8(record, PesTeamRecord.NameOffset, PesTeamRecord.NameCapacity), offset, record));
        }
        return result;
    }

    public static IReadOnlyList<PesPlayerAssignmentRecord> ParseAssignments(ReadOnlySpan<byte> payload)
    {
        var count = payload.Length / PesPlayerAssignmentRecord.RecordSize;
        var result = new List<PesPlayerAssignmentRecord>(count);
        for (var i = 0; i < count; i++)
        {
            var offset = i * PesPlayerAssignmentRecord.RecordSize;
            var record = payload.Slice(offset, PesPlayerAssignmentRecord.RecordSize).ToArray();
            result.Add(new PesPlayerAssignmentRecord(i,
                BinaryPrimitives.ReadUInt32LittleEndian(record.AsSpan(4, 4)),
                BinaryPrimitives.ReadUInt32LittleEndian(record.AsSpan(8, 4)),
                record[12], record[13], record[14], record));
        }
        return result;
    }

    public static IReadOnlyList<PesCompetitionRecord> ParseCompetitions(ReadOnlySpan<byte> payload)
    {
        var count = payload.Length / PesCompetitionRecord.RecordSize;
        var result = new List<PesCompetitionRecord>(count);
        for (var i = 0; i < count; i++)
        {
            var offset = i * PesCompetitionRecord.RecordSize;
            var record = payload.Slice(offset, PesCompetitionRecord.RecordSize).ToArray();
            result.Add(new PesCompetitionRecord(i, ReadFixedUtf8(record, PesCompetitionRecord.NameOffset, PesCompetitionRecord.NameCapacity), offset, record));
        }
        return result;
    }

    public static byte[] ReplacePlayerName(ReadOnlySpan<byte> payload, int playerIndex, string name)
    {
        var offset = checked(playerIndex * PesPlayerRecord.RecordSize + PesPlayerRecord.NameOffset);
        if (offset < 0 || offset > payload.Length - PesPlayerRecord.NameCapacity)
            throw new ArgumentOutOfRangeException(nameof(playerIndex));
        var result = payload.ToArray();
        WriteFixedUtf8(result, offset, PesPlayerRecord.NameCapacity, name);
        return result;
    }

    private static string ReadFixedUtf8(ReadOnlySpan<byte> record, int offset, int capacity)
    {
        var span = record.Slice(offset, capacity);
        var end = span.IndexOf((byte)0);
        if (end < 0) end = span.Length;
        return Encoding.UTF8.GetString(span[..end]).TrimEnd();
    }

    private static void WriteFixedUtf8(Span<byte> data, int offset, int capacity, string value)
    {
        var encoded = Encoding.UTF8.GetBytes(value);
        if (encoded.Length >= capacity)
            throw new InvalidDataException($"O nome excede a capacidade comprovada de {capacity - 1} bytes.");
        data.Slice(offset, capacity).Clear();
        encoded.CopyTo(data[offset..]);
    }
}
