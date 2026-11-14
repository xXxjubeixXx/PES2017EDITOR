"""Leitor e gravador CPK/UTF para arquivos de PES 2017."""
from __future__ import annotations

import datetime as dt
import os
import struct
from dataclasses import dataclass
from pathlib import Path


class CpkError(Exception):
    pass


def _crypt(data: bytes) -> bytes:
    key = 0x5F
    out = bytearray(data)
    for i in range(len(out)):
        out[i] ^= key
        key = (key * 0x15) & 0xFF
    return bytes(out)


_SIZES = {0: 1, 1: 1, 2: 2, 3: 2, 4: 4, 5: 4, 6: 8, 7: 8, 8: 4, 10: 4, 11: 8}


def _read_utf(file, offset: int, expected: bytes | None = None):
    file.seek(offset)
    header = file.read(16)
    if len(header) != 16:
        raise CpkError("Cabeçalho UTF incompleto")
    magic, _, size = struct.unpack("<4sIQ", header)
    if expected is not None and magic != expected:
        raise CpkError(f"Tabela inválida: esperado {expected!r}, encontrado {magic!r}")
    if size > 128 * 1024 * 1024:
        raise CpkError("Tabela UTF excede o limite de segurança (128 MB)")
    raw = file.read(size)
    if len(raw) != size:
        raise CpkError("Tabela UTF truncada")
    if raw[:4] != b"@UTF":
        raw = _crypt(raw)
    if raw[:4] != b"@UTF":
        raise CpkError("Não foi possível decodificar a tabela UTF")
    _, _, row_off, str_off, data_off, _, col_count, row_size, row_count = struct.unpack_from(">4sIIIIIHHI", raw)
    row_off += 8
    str_off += 8
    data_off += 8

    def string(index):
        start = str_off + index
        end = raw.find(b"\0", start)
        if start >= len(raw) or end < 0:
            raise CpkError("String UTF fora da tabela")
        return raw[start:end].decode("utf-8", errors="replace")

    def value(pos, kind):
        if kind not in _SIZES:
            raise CpkError(f"Tipo UTF não suportado: {kind}")
        fmt = {0: ">B", 1: ">b", 2: ">H", 3: ">h", 4: ">I", 5: ">i", 6: ">Q", 7: ">q", 8: ">f", 10: ">I"}.get(kind)
        if kind == 11:
            rel, length = struct.unpack_from(">II", raw, pos)
            return raw[data_off + rel:data_off + rel + length], pos + 8
        val = struct.unpack_from(fmt, raw, pos)[0]
        if kind == 10:
            val = string(val)
        return val, pos + _SIZES[kind]

    schema = []
    pos = 32
    for _ in range(col_count):
        flags, name_off = struct.unpack_from(">BI", raw, pos)
        pos += 5
        storage, kind = flags >> 4, flags & 15
        constant = None
        if storage == 3:
            constant, pos = value(pos, kind)
        schema.append((string(name_off), storage, kind, constant))
    rows = []
    if row_count > 10_000_000:
        raise CpkError("Quantidade excessiva de registros UTF")
    for i in range(row_count):
        pos = row_off + i * row_size
        row = {}
        for name, storage, kind, constant in schema:
            if storage == 5:
                row[name], pos = value(pos, kind)
            elif storage == 3:
                row[name] = constant
            elif storage == 1:
                row[name] = 0
            elif storage == 0:
                row[name] = None
            else:
                raise CpkError(f"Armazenamento UTF desconhecido: {storage}")
        rows.append(row)
    return rows


def _pack_utf(magic: bytes, name: str, columns, rows) -> bytes:
    strings, blobs = bytearray(), bytearray()
    indices = {}

    def add_string(value):
        value = "" if value is None else str(value)
        if value not in indices:
            indices[value] = len(strings)
            strings.extend(value.encode("utf-8") + b"\0")
        return indices[value]

    add_string(name)
    colbuf, rowbuf = bytearray(), bytearray()
    storage = {}
    for col_name, kind in columns:
        is_null = len(rows) == 1 and rows[0].get(col_name) is None
        storage[col_name] = 0 if is_null else 5
        colbuf.extend(struct.pack(">BI", (storage[col_name] << 4) | kind, add_string(col_name)))
    for row in rows:
        for col_name, kind in columns:
            if storage[col_name] == 0:
                continue
            val = row.get(col_name)
            if kind == 10:
                rowbuf.extend(struct.pack(">I", add_string(val)))
            elif kind == 11:
                val = bytes(val or b"")
                rel = len(blobs)
                blobs.extend(val)
                blobs.extend(bytes((-len(blobs)) % 8))
                rowbuf.extend(struct.pack(">II", rel, len(val)))
            else:
                fmt = {0: ">B", 1: ">b", 2: ">H", 3: ">h", 4: ">I", 5: ">i", 6: ">Q", 7: ">q", 8: ">f"}[kind]
                rowbuf.extend(struct.pack(fmt, val or 0))
    row_size = sum(_SIZES[k] for n, k in columns if storage[n] != 0)
    row_offset = 32 + len(colbuf)
    str_offset = row_offset + len(rowbuf)
    pad = bytes((-str_offset - len(strings)) % 8)
    data_offset = str_offset + len(strings) + len(pad)
    total = data_offset + len(blobs)
    header = struct.pack(">4sIIIIIHHI", b"@UTF", total - 8, row_offset - 8,
                         str_offset - 8, data_offset - 8, indices[name], len(columns), row_size, len(rows))
    payload = header + colbuf + rowbuf + strings + pad + blobs
    return struct.pack("<4sIQ", magic, 0, len(payload)) + _crypt(payload)


@dataclass
class Entry:
    name: str
    offset: int
    packed_size: int
    size: int


class CpkArchive:
    def __init__(self, filename):
        self.path = Path(filename)
        self.entries: list[Entry] = []
        with self.path.open("rb") as f:
            header = _read_utf(f, 0, b"CPK ")
            if not header:
                raise CpkError("CPK sem cabeçalho")
            fields = header[0]
            toc = _read_utf(f, fields["TocOffset"], b"TOC ")
            self.entries = [Entry(
                (str(r.get("DirName") or "").replace("\\", "/").strip("/") + "/" if r.get("DirName") else "") + str(r["FileName"]).replace("\\", "/").lstrip("/"),
                int(r["FileOffset"]) + 0x800, int(r["FileSize"]), int(r["ExtractSize"])) for r in toc]
        self._source = None

    def read(self, entry: Entry) -> bytes:
        with self.path.open("rb") as f:
            f.seek(entry.offset)
            data = f.read(entry.packed_size)
        if len(data) != entry.packed_size:
            raise CpkError(f"Arquivo truncado dentro do CPK: {entry.name}")
        if entry.size != entry.packed_size and data.startswith(b"CRILAYLA"):
            return decompress_crilayla(data)
        return data

    def extract_all(self, destination: Path, progress=None):
        root = destination.resolve()
        root.mkdir(parents=True, exist_ok=True)
        for i, entry in enumerate(self.entries, 1):
            rel = Path(entry.name.replace("/", os.sep))
            target = (root / rel).resolve()
            if root not in target.parents and target != root:
                raise CpkError(f"Caminho inseguro no CPK: {entry.name}")
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(self.read(entry))
            if progress:
                progress(i, len(self.entries), entry.name)

    @staticmethod
    def pack_folder(folder: Path, destination: Path, progress=None):
        folder = folder.resolve()
        files = sorted(p for p in folder.rglob("*") if p.is_file())
        if not files:
            raise CpkError("A pasta de conteúdo está vazia")
        temp = destination.with_suffix(destination.suffix + ".tmp")
        toc_cols = [("DirName", 10), ("FileName", 10), ("FileSize", 4), ("ExtractSize", 4), ("FileOffset", 6), ("ID", 4), ("UserString", 10)]
        etoc_cols = [("UpdateDateTime", 6), ("LocalDir", 10)]
        toc_rows, etoc_rows, file_rows = [], [], []
        position = 0x800
        try:
            with temp.open("wb") as out:
                out.write(bytes(0x7FA) + b"(c)CRI")
                for i, path in enumerate(files, 1):
                    rel = path.relative_to(folder).as_posix()
                    content = path.read_bytes()
                    name = rel.rsplit("/", 1)
                    dirname, filename = name if len(name) == 2 else ("", name[0])
                    offset = position
                    out.write(content)
                    pad = (-len(content)) % 0x800
                    out.write(bytes(pad))
                    position += len(content) + pad
                    toc_rows.append({"DirName": dirname, "FileName": filename, "FileSize": len(content), "ExtractSize": len(content), "FileOffset": offset - 0x800, "ID": i - 1, "UserString": ""})
                    ts = dt.datetime.fromtimestamp(path.stat().st_mtime)
                    encoded = (ts.year << 48) | (ts.month << 40) | (ts.day << 32) | (ts.hour << 24) | (ts.minute << 16) | (ts.second << 8)
                    etoc_rows.append({"UpdateDateTime": encoded, "LocalDir": dirname})
                    if progress:
                        progress(i, len(files), rel)
                toc_offset = position
                toc_data = _pack_utf(b"TOC ", "CpkTocInfo", toc_cols, toc_rows)
                out.write(toc_data)
                position += len(toc_data)
                pad = (-position) % 0x800
                out.write(bytes(pad))
                position += pad
                etoc_offset = position
                etoc_rows.append({"UpdateDateTime": 0, "LocalDir": ""})
                etoc_data = _pack_utf(b"ETOC", "CpkEtocInfo", etoc_cols, etoc_rows)
                out.write(etoc_data)
                header_cols = [("UpdateDateTime", 6), ("FileSize", 6), ("ContentOffset", 6), ("ContentSize", 6), ("TocOffset", 6), ("TocSize", 6), ("TocCrc", 4), ("HtocOffset", 6), ("HtocSize", 6), ("EtocOffset", 6), ("EtocSize", 6), ("ItocOffset", 6), ("ItocSize", 6), ("ItocCrc", 4), ("GtocOffset", 6), ("GtocSize", 6), ("GtocCrc", 4), ("HgtocOffset", 6), ("EnabledPackedSize", 6), ("EnabledDataSize", 6), ("TotalDataSize", 6), ("Tocs", 4), ("Files", 4), ("Groups", 4), ("Attrs", 4), ("TotalFiles", 4), ("Directories", 4), ("Updates", 4), ("Version", 2), ("Revision", 2), ("Align", 2), ("Sorted", 2), ("EnableFileName", 2), ("EID", 2), ("CpkMode", 4), ("Tvers", 10), ("Comment", 10), ("Codec", 4), ("DpkItoc", 4), ("EnableTocCrc", 2), ("EnableFileCrc", 2), ("CrcMode", 4), ("CrcTable", 11)]
                size = out.tell()
                values = {"UpdateDateTime": 1, "FileSize": size, "ContentOffset": 0x800, "ContentSize": toc_offset - 0x800, "TocOffset": toc_offset, "TocSize": len(toc_data), "TocCrc": 0, "HtocOffset": 0, "HtocSize": 0, "EtocOffset": etoc_offset, "EtocSize": len(etoc_data), "ItocOffset": 0, "ItocSize": 0, "ItocCrc": 0, "GtocOffset": 0, "GtocSize": 0, "GtocCrc": 0, "HgtocOffset": 0, "EnabledPackedSize": toc_offset - 0x800, "EnabledDataSize": toc_offset - 0x800, "TotalDataSize": toc_offset - 0x800, "Tocs": 1, "Files": len(files), "Groups": 0, "Attrs": 0, "TotalFiles": len(files), "Directories": 0, "Updates": 0, "Version": 7, "Revision": 14, "Align": 0x800, "Sorted": 1, "EnableFileName": 1, "EID": 0, "CpkMode": 1, "Tvers": "PES2017EDITOR", "Comment": "", "Codec": 0, "DpkItoc": 0, "EnableTocCrc": 0, "EnableFileCrc": 0, "CrcMode": 0, "CrcTable": b""}
                header = _pack_utf(b"CPK ", "CpkHeader", header_cols, [values])
                out.seek(0)
                out.write(header)
            temp.replace(destination)
        except Exception:
            temp.unlink(missing_ok=True)
            raise


def decompress_crilayla(data: bytes) -> bytes:
    """Decodifica o fluxo CRILAYLA utilizado por alguns arquivos do jogo."""
    if len(data) < 16 or not data.startswith(b"CRILAYLA"):
        raise CpkError("Cabeçalho CRILAYLA inválido")
    output_size, prefix_offset = struct.unpack_from("<II", data, 8)
    prefix_size = 0x100
    if output_size > 512 * 1024 * 1024 or 16 + prefix_offset + prefix_size > len(data):
        raise CpkError("Tamanho CRILAYLA inválido ou excessivo")
    prefix = data[16 + prefix_offset:16 + prefix_offset + prefix_size]
    bitstream = data[16:16 + prefix_offset]
    output = bytearray(output_size)
    bit_index = 0
    pooled = pooled_count = decoded = 0

    def read_bits(count):
        nonlocal bit_index, pooled, pooled_count
        while pooled_count < count:
            if bit_index >= len(bitstream):
                raise CpkError("Fluxo CRILAYLA truncado")
            pooled = (pooled << 8) | bitstream[len(bitstream) - bit_index - 1]
            pooled_count += 8
            bit_index += 1
        result = pooled >> (pooled_count - count)
        pooled_count -= count
        pooled &= (1 << pooled_count) - 1
        return result

    while decoded < output_size:
        if read_bits(1):
            distance = read_bits(13) + 3
            length = 3
            for chunk_size in (2, 3, 5, 8):
                chunk = read_bits(chunk_size)
                length += chunk
                if chunk + 1 != 1 << chunk_size:
                    break
            else:
                while True:
                    chunk = read_bits(8)
                    length += chunk
                    if chunk != 255:
                        break
            if length > output_size - decoded:
                raise CpkError("Comprimento CRILAYLA inválido")
            for _ in range(length):
                dest = output_size - decoded - 1
                source = dest + distance
                if source >= output_size:
                    raise CpkError("Referência CRILAYLA fora do fluxo")
                output[dest] = output[source]
                decoded += 1
        else:
            output[output_size - decoded - 1] = read_bits(8)
            decoded += 1
    return prefix + bytes(output)
