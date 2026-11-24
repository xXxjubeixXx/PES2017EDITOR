using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace PES2017Editor.Cpk;

public enum CriUtfType
{
    UInt8 = 0x00,
    Int8 = 0x01,
    UInt16 = 0x02,
    Int16 = 0x03,
    UInt32 = 0x04,
    Int32 = 0x05,
    UInt64 = 0x06,
    Int64 = 0x07,
    Float = 0x08,
    Double = 0x09,
    String = 0x0A,
    VariableData = 0x0B,
    UInt128 = 0x0C
}

public sealed record CriUtfColumn(string Name, byte Info, CriUtfType Type, byte[]? DefaultBytes, int RowOffset)
{
    public bool IsDefault => (Info & 0x20) != 0;
    public bool IsRowValue => (Info & 0x40) != 0;
    public int ValueSize => Type switch
    {
        CriUtfType.UInt8 or CriUtfType.Int8 => 1,
        CriUtfType.UInt16 or CriUtfType.Int16 => 2,
        CriUtfType.UInt32 or CriUtfType.Int32 or CriUtfType.Float or CriUtfType.String => 4,
        CriUtfType.UInt64 or CriUtfType.Int64 or CriUtfType.Double or CriUtfType.VariableData => 8,
        CriUtfType.UInt128 => 16,
        _ => throw new InvalidDataException($"Tipo CRI UTF não suportado: 0x{(byte)Type:X2}")
    };
}

public sealed class CriUtfTable
{
    private readonly byte[] _data;

    private CriUtfTable(byte[] data, string tableName, int rowsOffset, int stringsOffset, int dataOffset,
        int rowWidth, int rowCount, IReadOnlyList<CriUtfColumn> columns)
    {
        _data = data;
        TableName = tableName;
        TableSize = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(4, 4)) + 8;
        Version = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(8, 2));
        RowsOffset = rowsOffset;
        StringsOffset = stringsOffset;
        DataOffset = dataOffset;
        RowWidth = rowWidth;
        RowCount = rowCount;
        Columns = columns;
    }

    public string TableName { get; }
    public uint TableSize { get; }
    public ushort Version { get; }
    public int RowsOffset { get; }
    public int StringsOffset { get; }
    public int DataOffset { get; }
    public int RowWidth { get; }
    public int RowCount { get; }
    public IReadOnlyList<CriUtfColumn> Columns { get; }

    public static CriUtfTable Parse(ReadOnlySpan<byte> source)
    {
        if (source.Length < 0x20 || !source[..4].SequenceEqual("@UTF"u8))
            throw new InvalidDataException("Tabela CRI UTF ausente ou com assinatura inválida.");

        var tableSize = checked((int)BinaryPrimitives.ReadUInt32BigEndian(source.Slice(4, 4)) + 8);
        if (tableSize < 0x20 || tableSize > source.Length)
            throw new InvalidDataException($"Tamanho CRI UTF inválido: {tableSize} bytes.");

        var rowsOffset = checked((int)BinaryPrimitives.ReadUInt16BigEndian(source.Slice(0x0A, 2)) + 8);
        var stringsOffset = checked((int)BinaryPrimitives.ReadUInt32BigEndian(source.Slice(0x0C, 4)) + 8);
        var dataOffset = checked((int)BinaryPrimitives.ReadUInt32BigEndian(source.Slice(0x10, 4)) + 8);
        var nameOffset = checked((int)BinaryPrimitives.ReadUInt32BigEndian(source.Slice(0x14, 4)));
        var columnCount = BinaryPrimitives.ReadUInt16BigEndian(source.Slice(0x18, 2));
        var rowWidth = BinaryPrimitives.ReadUInt16BigEndian(source.Slice(0x1A, 2));
        var rowCount = checked((int)BinaryPrimitives.ReadUInt32BigEndian(source.Slice(0x1C, 4)));

        if (rowsOffset < 0x20 || rowsOffset > stringsOffset || stringsOffset > dataOffset || dataOffset > tableSize)
            throw new InvalidDataException("Seções CRI UTF fora de ordem ou fora dos limites da tabela.");
        if ((long)rowWidth * rowCount > stringsOffset - rowsOffset)
            throw new InvalidDataException("Linhas CRI UTF excedem a seção declarada.");

        var stringTable = source.Slice(stringsOffset, dataOffset - stringsOffset);
        var tableName = ReadString(stringTable, nameOffset);
        var schemaCursor = 0x20;
        var rowCursor = 0;
        var columns = new List<CriUtfColumn>(columnCount);
        for (var i = 0; i < columnCount; i++)
        {
            if (schemaCursor + 5 > rowsOffset)
                throw new InvalidDataException("Descritor CRI UTF truncado.");

            var info = source[schemaCursor++];
            var nameOffsetValue = checked((int)BinaryPrimitives.ReadUInt32BigEndian(source.Slice(schemaCursor, 4)));
            schemaCursor += 4;
            var type = (CriUtfType)(info & 0x0F);
            var columnName = ReadString(stringTable, nameOffsetValue);
            var defaultBytes = (byte[]?)null;
            var rowOffset = -1;
            var valueSize = ValueSize(type);
            var flags = info & 0xF0;
            if ((flags & 0x20) != 0)
            {
                if (schemaCursor + valueSize > rowsOffset)
                    throw new InvalidDataException("Valor padrão CRI UTF truncado.");
                defaultBytes = source.Slice(schemaCursor, valueSize).ToArray();
                schemaCursor += valueSize;
            }
            else if ((flags & 0x40) != 0)
            {
                rowOffset = rowCursor;
                rowCursor += valueSize;
            }
            else if (flags is not (0x10 or 0x50))
            {
                throw new InvalidDataException($"Flags CRI UTF não suportadas: 0x{flags:X2}.");
            }

            columns.Add(new CriUtfColumn(columnName, info, type, defaultBytes, rowOffset));
        }

        if (schemaCursor > rowsOffset || rowCursor > rowWidth)
            throw new InvalidDataException("Esquema CRI UTF excede a largura declarada.");

        return new CriUtfTable(source[..tableSize].ToArray(), tableName, rowsOffset, stringsOffset, dataOffset,
            rowWidth, rowCount, columns);
    }

    public IReadOnlyDictionary<string, object?> ReadRow(int rowIndex)
    {
        if ((uint)rowIndex >= (uint)RowCount)
            throw new ArgumentOutOfRangeException(nameof(rowIndex));

        var rowStart = RowsOffset + rowIndex * RowWidth;
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var column in Columns)
        {
            ReadOnlySpan<byte> bytes;
            if (column.IsDefault)
                bytes = column.DefaultBytes ?? ReadOnlySpan<byte>.Empty;
            else if (column.IsRowValue)
                bytes = _data.AsSpan(rowStart + column.RowOffset, column.ValueSize);
            else
                bytes = ReadOnlySpan<byte>.Empty;

            values[column.Name] = ReadValue(column.Type, bytes);
        }
        return values;
    }

    public IReadOnlyList<IReadOnlyDictionary<string, object?>> ReadRows(int maxRows = int.MaxValue)
    {
        var result = new List<IReadOnlyDictionary<string, object?>>(Math.Min(RowCount, maxRows));
        for (var i = 0; i < RowCount && i < maxRows; i++)
            result.Add(ReadRow(i));
        return result;
    }

    public ReadOnlyMemory<byte> ReadData(int offset, int size)
    {
        if (offset < 0 || size < 0 || offset > _data.Length - size)
            throw new ArgumentOutOfRangeException();
        return _data.AsMemory(offset, size);
    }

    private object? ReadValue(CriUtfType type, ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length == 0)
            return null;
        return type switch
        {
            CriUtfType.UInt8 => bytes[0],
            CriUtfType.Int8 => unchecked((sbyte)bytes[0]),
            CriUtfType.UInt16 => BinaryPrimitives.ReadUInt16BigEndian(bytes),
            CriUtfType.Int16 => BinaryPrimitives.ReadInt16BigEndian(bytes),
            CriUtfType.UInt32 => BinaryPrimitives.ReadUInt32BigEndian(bytes),
            CriUtfType.Int32 => BinaryPrimitives.ReadInt32BigEndian(bytes),
            CriUtfType.UInt64 => BinaryPrimitives.ReadUInt64BigEndian(bytes),
            CriUtfType.Int64 => BinaryPrimitives.ReadInt64BigEndian(bytes),
            CriUtfType.Float => BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32BigEndian(bytes)),
            CriUtfType.Double => BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64BigEndian(bytes)),
            CriUtfType.String => ReadStringFromStringTable(bytes),
            CriUtfType.VariableData => new CriUtfVariableData(BinaryPrimitives.ReadUInt32BigEndian(bytes), BinaryPrimitives.ReadUInt32BigEndian(bytes[4..])),
            CriUtfType.UInt128 => Convert.ToHexString(bytes),
            _ => Convert.ToHexString(bytes)
        };
    }

    private string? ReadStringFromStringTable(ReadOnlySpan<byte> bytes)
    {
        var offset = checked((int)BinaryPrimitives.ReadUInt32BigEndian(bytes));
        return ReadString(_data.AsSpan(StringsOffset, DataOffset - StringsOffset), offset);
    }

    private static int ValueSize(CriUtfType type) => type switch
    {
        CriUtfType.UInt8 or CriUtfType.Int8 => 1,
        CriUtfType.UInt16 or CriUtfType.Int16 => 2,
        CriUtfType.UInt32 or CriUtfType.Int32 or CriUtfType.Float or CriUtfType.String => 4,
        CriUtfType.UInt64 or CriUtfType.Int64 or CriUtfType.Double or CriUtfType.VariableData => 8,
        CriUtfType.UInt128 => 16,
        _ => throw new InvalidDataException($"Tipo CRI UTF desconhecido: 0x{(byte)type:X2}.")
    };

    private static string ReadString(ReadOnlySpan<byte> strings, int offset)
    {
        if ((uint)offset >= (uint)strings.Length)
            throw new InvalidDataException($"Offset de string CRI UTF fora da seção: {offset}.");
        var end = strings[offset..].IndexOf((byte)0);
        if (end < 0)
            throw new InvalidDataException("String CRI UTF sem terminador NUL.");
        return Encoding.UTF8.GetString(strings.Slice(offset, end));
    }
}

public sealed record CriUtfVariableData(uint Offset, uint Size)
{
    public override string ToString() => $"offset=0x{Offset:X}, size={Size}";
}
