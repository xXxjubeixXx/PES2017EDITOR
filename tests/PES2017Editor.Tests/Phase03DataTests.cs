using System.Text;
using PES2017Editor.Binary;
using PES2017Editor.Cpk;
using PES2017Editor.Database;
using PES2017Editor.Game;
using Xunit;

namespace PES2017Editor.Tests;

public sealed class Phase03DataTests
{
    [Fact]
    public void WesysZlibRoundTripPreservesPayload()
    {
        var payload = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("PES2017-EDITOR-", 200)));
        var header = new byte[16];
        "WESYS"u8.CopyTo(header.AsSpan(3));
        var template = new WesysDocument(header, payload, 3, true, 0, (uint)payload.Length);
        var encoded = WesysCodec.Encode(template, payload);
        var decoded = WesysCodec.Decode(encoded);
        Assert.True(decoded.IsCompressed);
        Assert.Equal(payload, decoded.Payload);
        Assert.Equal((uint)payload.Length, decoded.DeclaredPayloadSize);
    }

    [Fact]
    public void WesysDirectRoundTripPreservesPayload()
    {
        var payload = Encoding.UTF8.GetBytes("AFC_CHAMPIONS_LEAGUE\0");
        var header = new byte[16];
        "WESYS"u8.CopyTo(header.AsSpan(3));
        var template = new WesysDocument(header, payload, 3, false, 0, (uint)payload.Length);
        var encoded = WesysCodec.Encode(template, payload);
        var decoded = WesysCodec.Decode(encoded);
        Assert.False(decoded.IsCompressed);
        Assert.Equal(payload, decoded.Payload);
    }

    [Fact]
    public void PlayerNameEditPreservesBytesOutsideProvenField()
    {
        var payload = new byte[PesPlayerRecord.RecordSize * 2];
        BitConverter.GetBytes(1234u).CopyTo(payload, PesPlayerRecord.IdOffset);
        Encoding.UTF8.GetBytes("Original").CopyTo(payload, PesPlayerRecord.NameOffset);
        var updated = PesDatabaseParser.ReplacePlayerName(payload, 0, "Changed");
        Assert.Equal(1234u, BitConverter.ToUInt32(updated, PesPlayerRecord.IdOffset));
        Assert.Equal("Changed", PesDatabaseParser.ParsePlayers(updated)[0].Name);
        Assert.Equal(payload.AsSpan(PesPlayerRecord.NameOffset + PesPlayerRecord.NameCapacity),
            updated.AsSpan(PesPlayerRecord.NameOffset + PesPlayerRecord.NameCapacity));
    }

    [Fact]
    public async Task ExperimentalWriterChangesOnlyTheRequestedSlot()
    {
        var root = Path.Combine(Path.GetTempPath(), "pes2017-editor-cpk-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var source = Path.Combine(root, "source.cpk");
        var destination = Path.Combine(root, "copy.cpk");
        var bytes = Enumerable.Repeat((byte)0x5A, 4096).ToArray();
        await File.WriteAllBytesAsync(source, bytes);
        var entry = new CpkEntry(source, "common/etc/pesdb/Player.bin", 512, 1024, 1024, 0, null, "common/etc/pesdb", "Player.bin");
        var replacement = Enumerable.Repeat((byte)0x22, 500).ToArray();
        await new ExperimentalCpkWriter().CreateLayoutPreservingCopyAsync(entry, replacement, destination);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(source));
        var result = await File.ReadAllBytesAsync(destination);
        Assert.Equal(bytes[..512], result[..512]);
        Assert.Equal(replacement, result[512..(512 + replacement.Length)]);
        Assert.Equal(bytes[(512 + (int)entry.StoredSize)..], result[(512 + (int)entry.StoredSize)..]);
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    [Trait("Category", "Proprietary")]
    public async Task ReadsRealPesDatabaseEntities()
    {
        var executable = FindWorkspaceFile("originals/Pro Evolution Soccer 2017/PES2017.exe");
        var installation = await new GameInstallationDetector().DetectAsync(Path.GetDirectoryName(executable)!);
        var index = await new CpkIndexService().IndexAsync(installation);
        var snapshot = await new PesDatabaseReader().ReadAsync(index, new CpkArchiveReader());
        Assert.Equal(55, index.Archives.Count);
        Assert.Equal(40_505, index.Entries.Count);
        Assert.NotEmpty(snapshot.Players);
        Assert.NotEmpty(snapshot.Teams);
        Assert.NotEmpty(snapshot.Competitions);
        Assert.NotEmpty(snapshot.Assignments);
        Assert.NotNull(snapshot.PlayerDocument);
        Assert.NotNull(snapshot.TeamDocument);
    }

    [Fact]
    [Trait("Category", "Proprietary")]
    public async Task ReadsMaskedUtfCpkAndUsesTheEffectiveContentBase()
    {
        var path = FindWorkspaceFile("originals/Pro Evolution Soccer 2017/Data/dt20_bra_win.cpk");
        var archive = await new CpkArchiveReader().ReadAsync(path);

        Assert.True(archive.HasNativeIndex, string.Join(Environment.NewLine, archive.Warnings));
        Assert.Equal(5, archive.Entries.Count);
        var entry = Assert.Single(archive.Entries, x => x.InternalPath.EndsWith("cmnIconPositionGmPlan.bin", StringComparison.OrdinalIgnoreCase));
        var bytes = await new CpkArchiveReader().ReadEntryAsync(entry);
        Assert.Equal("FF105157455359", Convert.ToHexString(bytes.AsSpan(0, 7)));
    }

    [Fact]
    [Trait("Category", "Proprietary")]
    public async Task CreatesAndReopensRealExperimentalPlayerCpk()
    {
        var executable = FindWorkspaceFile("originals/Pro Evolution Soccer 2017/PES2017.exe");
        var installation = await new GameInstallationDetector().DetectAsync(Path.GetDirectoryName(executable)!);
        var index = await new CpkIndexService().IndexAsync(installation);
        var snapshot = await new PesDatabaseReader().ReadAsync(index, new CpkArchiveReader());
        var document = snapshot.PlayerDocument;
        Assert.NotNull(document);
        var original = snapshot.Players.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Name));
        Assert.NotNull(original);
        var newName = "PES17_EDITOR_TEST";
        var payload = PesDatabaseParser.ReplacePlayerName(document!.Wesys.Payload, original.Index, newName);
        var replacement = WesysCodec.Encode(document.Wesys, payload);
        Assert.True(replacement.Length <= document.Entry.StoredSize, "A edição experimental não coube no slot preservado.");
        var root = Path.Combine(Path.GetTempPath(), "pes2017-editor-real-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var outputPath = Path.Combine(root, "dt10-player-test.cpk");
            await new ExperimentalCpkWriter().CreateLayoutPreservingCopyAsync(document.Entry, replacement, outputPath);
            var reopened = await new CpkArchiveReader().ReadAsync(outputPath);
            var entry = Assert.Single(reopened.Entries, x => x.InternalPath.Equals(document.Entry.InternalPath, StringComparison.OrdinalIgnoreCase));
            var reopenedPlayers = PesDatabaseParser.ParsePlayers(WesysCodec.Decode(await new CpkArchiveReader().ReadEntryAsync(entry)).Payload);
            Assert.Equal(newName, reopenedPlayers[original!.Index].Name);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static string FindWorkspaceFile(string relative)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, relative.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(candidate)) return candidate;
            current = current.Parent;
        }
        throw new FileNotFoundException($"Fixture proprietário não disponível: {relative}");
    }
}
