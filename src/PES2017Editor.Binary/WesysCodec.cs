using System.Buffers.Binary;
using System.IO.Compression;

namespace PES2017Editor.Binary;

/// <summary>
/// Reads and writes the WESYS envelope observed in PES 2017 database BINs.
/// The envelope is known; the payload remains an opaque byte sequence to this layer.
/// </summary>
public sealed record WesysDocument(
    byte[] OriginalBytes,
    byte[] Payload,
    int MagicOffset,
    bool IsCompressed,
    uint DeclaredStoredSize,
    uint DeclaredPayloadSize)
{
    public int PayloadOffset => 16;
}

public static class WesysCodec
{
    public static bool IsWesys(ReadOnlySpan<byte> bytes) =>
        bytes.Length >= 16 &&
        ((bytes.Length >= 8 && bytes.Slice(1, 5).SequenceEqual("WESYS"u8)) ||
         (bytes.Length >= 8 && bytes.Slice(3, 5).SequenceEqual("WESYS"u8)));

    public static WesysDocument Decode(ReadOnlySpan<byte> bytes)
    {
        if (!IsWesys(bytes))
            throw new InvalidDataException("O BIN não contém um envelope WESYS reconhecido.");

        var magicOffset = bytes.Slice(1).IndexOf("WESYS"u8);
        if (magicOffset >= 0) magicOffset++;
        if (magicOffset < 0) magicOffset = bytes.Slice(3).IndexOf("WESYS"u8) + 3;
        if (magicOffset < 0 || bytes.Length < 16)
            throw new InvalidDataException("Envelope WESYS truncado.");

        var storedSize = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(8, 4));
        var payloadSize = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(12, 4));
        var available = bytes.Length - 16;
        var logicalStored = storedSize == 0 ? available : checked((int)Math.Min(storedSize, (uint)available));
        var stored = bytes.Slice(16, logicalStored).ToArray();
        var compressed = bytes[2] == 0x81;
        byte[] payload;
        if (compressed)
        {
            using var input = new MemoryStream(stored, writable: false);
            using var zlib = new ZLibStream(input, CompressionMode.Decompress, leaveOpen: false);
            using var output = new MemoryStream(payloadSize > 0 && payloadSize <= int.MaxValue ? (int)payloadSize : 0);
            zlib.CopyTo(output);
            payload = output.ToArray();
        }
        else
        {
            payload = stored;
        }

        if (payloadSize != 0 && payload.Length != payloadSize)
            throw new InvalidDataException($"WESYS declarou {payloadSize} bytes, mas produziu {payload.Length}.");

        return new WesysDocument(bytes.ToArray(), payload, magicOffset, compressed, storedSize, payloadSize);
    }

    public static byte[] Encode(WesysDocument template, ReadOnlySpan<byte> payload,
        CompressionLevel compressionLevel = CompressionLevel.Optimal)
    {
        var result = new byte[16];
        template.OriginalBytes.AsSpan(0, Math.Min(16, template.OriginalBytes.Length)).CopyTo(result);
        byte[] stored;
        if (template.IsCompressed)
        {
            using var output = new MemoryStream();
            using (var zlib = new ZLibStream(output, compressionLevel, leaveOpen: true))
                zlib.Write(payload);
            stored = output.ToArray();
            result[2] = 0x81;
        }
        else
        {
            stored = payload.ToArray();
            result[2] = 0x00;
        }

        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(8, 4), checked((uint)stored.Length));
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(12, 4), checked((uint)payload.Length));
        return [.. result, .. stored];
    }
}
