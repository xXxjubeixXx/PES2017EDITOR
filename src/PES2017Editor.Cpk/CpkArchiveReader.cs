using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace PES2017Editor.Cpk;

public sealed class CpkArchiveReader : ICpkArchiveBackend
{
    private static readonly byte[][] ChunkSignatures =
    ["TOC "u8.ToArray(), "ITOC"u8.ToArray(), "ETOC"u8.ToArray(), "GTOC"u8.ToArray()];

    public async Task<CpkArchive> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(path);
        try
        {
            await using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite,
                128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
            if (stream.Length < 4)
                return Invalid(fullPath, stream.Length, "Arquivo menor que um cabeçalho CPK.");

            var signature = await ReadAtAsync(stream, 0, 4, cancellationToken);
            if (!signature.Span.SequenceEqual("CPK "u8))
                return Invalid(fullPath, stream.Length, "Assinatura CPK ausente.");

            var warnings = new List<string>();
            var headerValues = new Dictionary<string, object?>(StringComparer.Ordinal);
            CriUtfTable? headerTable = null;
            try
            {
                headerTable = await ReadUtfAtAsync(stream, 0x10, cancellationToken);
                if (headerTable.RowCount > 0)
                    foreach (var pair in headerTable.ReadRow(0))
                        headerValues[pair.Key] = pair.Value;
            }
            catch (Exception ex) when (ex is InvalidDataException or IOException)
            {
                warnings.Add($"Cabeçalho CRI UTF não decodificado: {ex.Message}");
            }

            var chunks = await FindChunksAsync(stream, headerValues, cancellationToken);
            var toc = chunks.FirstOrDefault(x => x.FourCc == "TOC " && x.HasUtfTable);
            if (toc is null)
            {
                var reason = chunks.Any(x => x.FourCc == "TOC ")
                    ? "TOC detectada, mas mascarada/sem tabela @UTF decodificável."
                    : "TOC CPK não localizada em uma região suportada.";
                warnings.Add(reason);
                return new CpkArchive(fullPath, stream.Length, false, headerTable?.TableName, headerValues,
                    Array.Empty<CpkEntry>(), warnings, reason);
            }

            var tocTable = await ReadUtfAtAsync(stream, toc.Offset + 16, cancellationToken);
            var entries = BuildEntries(tocTable, fullPath, stream.Length, warnings);
            return new CpkArchive(fullPath, stream.Length, true, headerTable?.TableName, headerValues,
                entries, warnings, null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return Invalid(fullPath, File.Exists(fullPath) ? new FileInfo(fullPath).Length : 0, ex.Message);
        }
    }

    public async Task<byte[]> ReadEntryAsync(CpkEntry entry, CancellationToken cancellationToken = default)
    {
        if (entry.StoredSize < 0 || entry.StoredSize > int.MaxValue)
            throw new InvalidDataException("Entrada grande demais para extração em memória nesta versão.");
        await using var stream = new FileStream(entry.ArchivePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite,
            128 * 1024, FileOptions.Asynchronous | FileOptions.RandomAccess);
        return (await ReadAtAsync(stream, entry.FileOffset, checked((int)entry.StoredSize), cancellationToken)).ToArray();
    }

    public async Task<byte[]> ReadEntryPreviewAsync(CpkEntry entry, int maxBytes = 256, CancellationToken cancellationToken = default)
    {
        var count = checked((int)Math.Min(entry.StoredSize, Math.Max(1, maxBytes)));
        await using var stream = new FileStream(entry.ArchivePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite,
            64 * 1024, FileOptions.Asynchronous | FileOptions.RandomAccess);
        return (await ReadAtAsync(stream, entry.FileOffset, count, cancellationToken)).ToArray();
    }

    public async Task ExtractEntryAsync(CpkEntry entry, string destinationPath, CancellationToken cancellationToken = default)
    {
        var destination = Path.GetFullPath(destinationPath);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        await using var source = new FileStream(entry.ArchivePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite,
            128 * 1024, FileOptions.Asynchronous | FileOptions.RandomAccess);
        source.Position = entry.FileOffset;
        await using var target = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None,
            128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await CopyExactlyAsync(source, target, entry.StoredSize, cancellationToken);
    }

    public static bool LooksLikeWesys(ReadOnlySpan<byte> data)
    {
        return (data.Length >= 8 && data.Slice(1, 5).SequenceEqual("WESYS"u8)) ||
               (data.Length >= 8 && data.Slice(3, 5).SequenceEqual("WESYS"u8));
    }

    private static IReadOnlyList<CpkEntry> BuildEntries(CriUtfTable table, string archivePath, long archiveSize, List<string> warnings)
    {
        var rows = table.ReadRows();
        var entries = new List<CpkEntry>(rows.Count);
        foreach (var row in rows)
        {
            var directory = GetString(row, "DirName") ?? GetString(row, "Directory") ?? string.Empty;
            var fileName = GetString(row, "FileName") ?? GetString(row, "ContentFilename") ?? GetString(row, "File") ?? string.Empty;
            var internalPath = string.Join('/', new[] { directory.Trim('/', '\\'), fileName.Trim('/', '\\') }.Where(x => !string.IsNullOrWhiteSpace(x)));
            var offset = GetInt64(row, "FileOffset", "Offset", "ContentOffset");
            var storedSize = GetInt64(row, "Filesize", "FileSize", "StoredSize", "ContentSize");
            var originalSize = GetInt64(row, "ExtractSize", "Extractsize", "OriginalSize", "DataSize");
            var codec = (int)GetInt64(row, "Codec", "Compression", "CompressCodec");
            var id = GetValue(row, "ID", "FileId", "Id")?.ToString();

            if (string.IsNullOrWhiteSpace(internalPath) || offset < 0 || storedSize < 0 || offset > archiveSize - storedSize)
            {
                warnings.Add("Uma entrada TOC foi ignorada por caminho ou intervalo inválido.");
                continue;
            }
            entries.Add(new CpkEntry(archivePath, internalPath.Replace('\\', '/'), offset, storedSize,
                originalSize > 0 ? originalSize : storedSize, codec, id, directory, fileName));
        }
        return entries;
    }

    private static async Task<IReadOnlyList<CpkChunk>> FindChunksAsync(FileStream stream,
        IReadOnlyDictionary<string, object?> headerValues, CancellationToken cancellationToken)
    {
        var offsets = new HashSet<long>();
        foreach (var field in new[] { "TocOffset", "ItocOffset", "EtocOffset", "GtocOffset" })
        {
            var value = GetInt64(headerValues, field);
            if (value > 0 && value < stream.Length)
                offsets.Add(value);
        }

        var scanLength = Math.Min(stream.Length, 8L * 1024 * 1024);
        var scan = await ReadAtAsync(stream, 0, checked((int)scanLength), cancellationToken);
        foreach (var signature in ChunkSignatures)
        {
            var start = 0;
            while (start <= scan.Length - 4)
            {
                var index = scan.Span[start..].IndexOf(signature);
                if (index < 0) break;
                var absolute = start + index;
                offsets.Add(absolute);
                start = absolute + 1;
            }
        }

        var chunks = new List<CpkChunk>();
        foreach (var offset in offsets.OrderBy(x => x))
        {
            if (offset + 16 > stream.Length) continue;
            var head = await ReadAtAsync(stream, offset, 16, cancellationToken);
            var fourCc = Encoding.ASCII.GetString(head.Span[..4]);
            if (!ChunkSignatures.Any(x => x.AsSpan().SequenceEqual(head.Span[..4]))) continue;
            var size = BinaryPrimitives.ReadUInt32LittleEndian(head.Span.Slice(8, 4));
            var hasUtf = offset + 20 <= stream.Length &&
                (await ReadAtAsync(stream, offset + 16, 4, cancellationToken)).Span.SequenceEqual("@UTF"u8);
            chunks.Add(new CpkChunk(fourCc, offset, size, hasUtf, hasUtf ? null : "Payload da tabela não começa com @UTF."));
        }
        return chunks;
    }

    private static async Task<CriUtfTable> ReadUtfAtAsync(FileStream stream, long offset, CancellationToken cancellationToken)
    {
        var header = await ReadAtAsync(stream, offset, 0x20, cancellationToken);
        if (!header.Span[..4].SequenceEqual("@UTF"u8))
            throw new InvalidDataException($"@UTF não encontrado em 0x{offset:X}; bytes={Convert.ToHexString(header.Span[..4])}.");
        var tableSize = checked((int)BinaryPrimitives.ReadUInt32BigEndian(header.Span.Slice(4, 4)) + 8);
        if (tableSize < 0x20 || tableSize > 128 * 1024 * 1024 || offset > stream.Length - tableSize)
            throw new InvalidDataException($"Tabela @UTF inválida em 0x{offset:X}: {tableSize} bytes.");
        var table = await ReadAtAsync(stream, offset, tableSize, cancellationToken);
        return CriUtfTable.Parse(table.Span);
    }

    private static async Task<ReadOnlyMemory<byte>> ReadAtAsync(FileStream stream, long offset, int count, CancellationToken cancellationToken)
    {
        var buffer = new byte[count];
        stream.Position = offset;
        var read = 0;
        while (read < count)
        {
            var n = await stream.ReadAsync(buffer.AsMemory(read, count - read), cancellationToken);
            if (n == 0) throw new EndOfStreamException("Fim inesperado durante leitura do CPK.");
            read += n;
        }
        return buffer;
    }

    private static async Task CopyExactlyAsync(Stream source, Stream target, long count, CancellationToken cancellationToken)
    {
        var buffer = new byte[128 * 1024];
        var remaining = count;
        while (remaining > 0)
        {
            var requested = (int)Math.Min(buffer.Length, remaining);
            var read = await source.ReadAsync(buffer.AsMemory(0, requested), cancellationToken);
            if (read == 0) throw new EndOfStreamException("Entrada CPK truncada durante extração.");
            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            remaining -= read;
        }
    }

    private static CpkArchive Invalid(string path, long size, string reason) =>
        new(path, size, false, null, new Dictionary<string, object?>(), Array.Empty<CpkEntry>(), [reason], reason);

    private static object? GetValue(IReadOnlyDictionary<string, object?> row, params string[] names)
    {
        foreach (var name in names)
            if (row.TryGetValue(name, out var value) && value is not null)
                return value;
        return null;
    }

    private static string? GetString(IReadOnlyDictionary<string, object?> row, string name) => GetValue(row, name)?.ToString();

    private static long GetInt64(IReadOnlyDictionary<string, object?> row, params string[] names)
    {
        var value = GetValue(row, names);
        if (value is null) return 0;
        return value switch
        {
            byte b => b,
            sbyte b => b,
            ushort s => s,
            short s => s,
            uint i => i,
            int i => i,
            ulong l => checked((long)l),
            long l => l,
            _ when long.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) => result,
            _ => 0
        };
    }
}
