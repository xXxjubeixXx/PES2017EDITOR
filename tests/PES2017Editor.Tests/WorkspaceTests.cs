using Microsoft.Data.Sqlite;
using PES2017Editor.Binary;
using PES2017Editor.Cpk;
using PES2017Editor.Database;
using PES2017Editor.Game;
using Xunit;

namespace PES2017Editor.Tests;

public sealed class WorkspaceTests
{
    [Fact]
    [Trait("Category", "Proprietary")]
    public async Task ReadsNativeCpkHeaderAndTocFromRealDt10()
    {
        var path = FindWorkspaceFile("originals/Pro Evolution Soccer 2017/Data/dt10_win.cpk");
        var archive = await new CpkArchiveReader().ReadAsync(path);

        Assert.True(archive.HasNativeIndex, string.Join(Environment.NewLine, archive.Warnings));
        Assert.Equal("CpkHeader", archive.HeaderTableName);
        Assert.NotEmpty(archive.Entries);
        Assert.Contains(archive.Entries, entry => entry.InternalPath.EndsWith("Competition.bin", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Category", "Proprietary")]
    public async Task ExtractsOneEntryWithoutChangingTheCpk()
    {
        var path = FindWorkspaceFile("originals/Pro Evolution Soccer 2017/Data/dt10_win.cpk");
        var before = await File.ReadAllBytesAsync(path);
        var archive = await new CpkArchiveReader().ReadAsync(path);
        var entry = Assert.Single(archive.Entries, x => x.InternalPath.EndsWith("Competition.bin", StringComparison.OrdinalIgnoreCase));
        var bytes = await new CpkArchiveReader().ReadEntryAsync(entry);
        Assert.NotEmpty(bytes);
        Assert.Equal(before, await File.ReadAllBytesAsync(path));
    }

    [Fact]
    [Trait("Category", "Proprietary")]
    public async Task IndexesAllRealInstallationCpksWithoutExtractingThem()
    {
        var executable = FindWorkspaceFile("originals/Pro Evolution Soccer 2017/PES2017.exe");
        var installation = await new GameInstallationDetector().DetectAsync(Path.GetDirectoryName(executable)!);
        var result = await new CpkIndexService().IndexAsync(installation);

        Assert.Equal(55, result.Archives.Count);
        Assert.Equal(159, result.Entries.Count);
        Assert.Contains(result.Archives, archive => archive.FileName.Equals("dt10_win.cpk", StringComparison.OrdinalIgnoreCase) && archive.HasNativeIndex);
        Assert.Contains(result.Warnings, warning => warning.Contains("mascarada", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Category", "Proprietary")]
    public async Task DetectsInstallationWithoutAssumingDatabaseCounts()
    {
        var executable = FindWorkspaceFile("originals/Pro Evolution Soccer 2017/PES2017.exe");
        var info = await new GameInstallationDetector().DetectAsync(Path.GetDirectoryName(executable)!);
        Assert.True(info.HasDataDirectory);
        Assert.NotNull(info.ExecutablePath);
        Assert.NotEmpty(info.CpkPaths);
    }

    [Fact]
    public void UnknownBinaryRemainsRawAndWesysEnvelopeIsRecognized()
    {
        var registry = new BinaryParserRegistry();
        var unknown = registry.Inspect([0x01, 0x02, 0x03]);
        Assert.Equal("raw", unknown.ParserId);
        var wesys = new byte[16];
        "WESYS"u8.CopyTo(wesys.AsSpan(1));
        var known = registry.Inspect(wesys);
        Assert.Equal("wesys-envelope", known.ParserId);
    }

    [Fact]
    [Trait("Category", "Proprietary")]
    public async Task PersistsIndexInLocalSqliteCache()
    {
        var path = FindWorkspaceFile("originals/Pro Evolution Soccer 2017/Data/dt10_win.cpk");
        var archive = await new CpkArchiveReader().ReadAsync(path);
        var root = Path.Combine(Path.GetTempPath(), "pes2017-editor-test-" + Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(root, "index.db");
        try
        {
            await new SqliteIndexStore(databasePath).SaveAsync(
                new CpkIndexResult([archive], archive.Entries, archive.Warnings));
            Assert.True(File.Exists(databasePath));
            Assert.True(new FileInfo(databasePath).Length > 0);
            SqliteConnection.ClearAllPools();
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
