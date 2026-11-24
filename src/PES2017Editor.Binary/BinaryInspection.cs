using PES2017Editor.Cpk;

namespace PES2017Editor.Binary;

public sealed record HexLine(long Offset, string Hex, string Ascii);

public sealed record BinaryPreview(
    string SourceName,
    long Length,
    string Signature,
    IReadOnlyList<HexLine> Lines,
    string? DetectedEnvelope,
    string? Notes);

public sealed record BinaryField(string Name, string Value, string Confidence);

public sealed record BinaryInspection(
    string ParserId,
    string Format,
    IReadOnlyList<BinaryField> Fields,
    string? Notes,
    IReadOnlyList<string> Warnings);

public interface IBinaryParser
{
    string Id { get; }
    bool CanParse(ReadOnlySpan<byte> data, string? internalPath = null);
    BinaryInspection Inspect(ReadOnlySpan<byte> data, string? internalPath = null);
}

public sealed class BinaryParserRegistry
{
    private readonly IReadOnlyList<IBinaryParser> _parsers;

    public BinaryParserRegistry(IEnumerable<IBinaryParser>? parsers = null)
    {
        _parsers = (parsers ?? [new WesysParser()]).ToArray();
    }

    public BinaryInspection Inspect(ReadOnlySpan<byte> data, string? internalPath = null)
    {
        IBinaryParser? parser = null;
        foreach (var candidate in _parsers)
        {
            if (candidate.CanParse(data, internalPath))
            {
                parser = candidate;
                break;
            }
        }
        return parser?.Inspect(data, internalPath) ?? new BinaryInspection("raw", "BIN não identificado",
            [new BinaryField("Tamanho", data.Length.ToString(), "comprovado")],
            "Nenhum parser específico foi aplicado; bytes desconhecidos foram preservados.", Array.Empty<string>());
    }
}

public sealed class WesysParser : IBinaryParser
{
    public string Id => "wesys-envelope";
    public bool CanParse(ReadOnlySpan<byte> data, string? internalPath = null) => CpkArchiveReader.LooksLikeWesys(data);

    public BinaryInspection Inspect(ReadOnlySpan<byte> data, string? internalPath = null)
    {
        var magicOffset = data.Slice(1).IndexOf("WESYS"u8);
        if (magicOffset >= 0) magicOffset++;
        if (magicOffset < 0 && data.Length >= 8)
        {
            var alternateOffset = data.Slice(3).IndexOf("WESYS"u8);
            if (alternateOffset >= 0) magicOffset = alternateOffset + 3;
        }
        var payloadSize = data.Length >= 12 ? BitConverter.ToUInt32(data.Slice(8, 4)) : 0;
        var decompressedSize = data.Length >= 16 ? BitConverter.ToUInt32(data.Slice(12, 4)) : 0;
        return new BinaryInspection(Id, "WESYS", [
            new("Assinatura", $"offset 0x{magicOffset:X}", "comprovado"),
            new("Payload armazenado", payloadSize.ToString(), "comprovado pelo invólucro"),
            new("Tamanho descomprimido declarado", decompressedSize.ToString(), "comprovado pelo invólucro")
        ], "A semântica dos campos internos depende do arquivo BIN e não foi inventada.", Array.Empty<string>());
    }
}

public static class BinaryPreviewService
{
    public static BinaryPreview Create(ReadOnlySpan<byte> data, string sourceName, long length, int width = 16)
    {
        var lines = new List<HexLine>((data.Length + width - 1) / width);
        for (var offset = 0; offset < data.Length; offset += width)
        {
            var chunk = data.Slice(offset, Math.Min(width, data.Length - offset));
            var hex = string.Join(' ', chunk.ToArray().Select(x => x.ToString("X2"))).PadRight(width * 3 - 1);
            var ascii = new string(chunk.ToArray().Select(x => x is >= 32 and <= 126 ? (char)x : '.').ToArray());
            lines.Add(new HexLine(offset, hex, ascii));
        }

        var signature = data.Length == 0 ? string.Empty : string.Join(' ', data[..Math.Min(8, data.Length)].ToArray().Select(x => x.ToString("X2")));
        var envelope = CpkArchiveReader.LooksLikeWesys(data) ? "WESYS" : null;
        return new BinaryPreview(sourceName, length, signature, lines, envelope,
            "Visualização limitada aos bytes lidos; o restante do arquivo permanece intocado.");
    }
}
