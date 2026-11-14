"""Edição conservadora de nomes e registros do banco WESYS do PES 2017."""
from __future__ import annotations

import csv
import struct
import zlib
from dataclasses import dataclass
from pathlib import Path

from .cpk import CpkArchive, CpkError


PLAYER_RECORD_SIZE = 192
TEAM_RECORD_SIZE = 1400
ASSIGNMENT_RECORD_SIZE = 16
PLAYER_ID_OFFSET = 0x08
PLAYER_NAME_OFFSET = 0x62
PLAYER_SHIRT_OFFSET = 0x90
TEAM_ENGLISH_NAME_OFFSET = 0xEA
PLAYER_NAME_SIZE = 48
TEAM_NAME_SIZE = 70


class DatabaseError(Exception):
    pass


def decode_wesys(blob: bytes) -> tuple[bytes, bytes]:
    if len(blob) < 16 or blob[3:8] != b"WESYS":
        raise DatabaseError("O arquivo não tem cabeçalho WESYS esperado pelo PES 2017")
    packed_size, raw_size = struct.unpack_from("<II", blob, 8)
    if packed_size != len(blob) - 16 or raw_size > 128 * 1024 * 1024:
        raise DatabaseError("Tamanhos do cabeçalho WESYS não conferem")
    try:
        raw = zlib.decompress(blob[16:])
    except zlib.error as exc:
        raise DatabaseError(f"Não foi possível descompactar o banco WESYS: {exc}") from exc
    if len(raw) != raw_size:
        raise DatabaseError("O tamanho descompactado não confere com o cabeçalho WESYS")
    return blob[:8], raw


def encode_wesys(prefix: bytes, raw: bytes) -> bytes:
    packed = zlib.compress(raw, 9)
    return prefix + struct.pack("<II", len(packed), len(raw)) + packed


def _read_fixed(raw: bytes, offset: int, size: int) -> str:
    field = raw[offset:offset + size]
    return field.split(b"\0", 1)[0].decode("utf-8", errors="replace")


def _write_fixed(raw: bytearray, offset: int, size: int, value: str):
    encoded = value.encode("utf-8")
    if not value.strip():
        raise DatabaseError("O nome não pode ficar vazio")
    if len(encoded) >= size:
        raise DatabaseError(f"O nome excede o limite de {size - 1} bytes UTF-8")
    raw[offset:offset + size] = encoded + bytes(size - len(encoded))


@dataclass
class Player:
    index: int
    player_id: int
    name: str
    shirt_name: str


@dataclass
class Team:
    index: int
    team_id: int
    name: str


class DatabaseWorkspace:
    """Abre Player.bin, Team.bin e PlayerAssignment.bin direto de um CPK."""

    PATHS = {
        "players": "common/etc/pesdb/Player.bin",
        "teams": "common/etc/pesdb/Team.bin",
        "assignments": "common/etc/pesdb/PlayerAssignment.bin",
        "competitions": "common/etc/pesdb/Competition.bin",
    }

    def __init__(self, cpk_path: Path):
        self.archive = CpkArchive(cpk_path)
        by_name = {e.name.replace("\\", "/").casefold(): e for e in self.archive.entries}
        self.prefixes = {}
        self.raw = {}
        for key, relative in self.PATHS.items():
            entry = by_name.get(relative.casefold())
            if entry is None:
                entry = next((e for e in self.archive.entries if e.name.replace("\\", "/").casefold().endswith("/" + relative.casefold())), None)
            if entry is None or not entry.size:
                raise DatabaseError(f"Não encontrei {relative} neste CPK")
            self.prefixes[key], self.raw[key] = decode_wesys(self.archive.read(entry))
        if len(self.raw["players"]) % PLAYER_RECORD_SIZE:
            raise DatabaseError("Player.bin não tem registros de 192 bytes; versão ou DLC diferente")
        if len(self.raw["teams"]) % TEAM_RECORD_SIZE:
            raise DatabaseError("Team.bin não tem registros de 1400 bytes; versão ou DLC diferente")
        if len(self.raw["assignments"]) % ASSIGNMENT_RECORD_SIZE:
            raise DatabaseError("PlayerAssignment.bin não tem registros de 16 bytes")
        if len(self.raw["competitions"]) % 32:
            raise DatabaseError("Competition.bin não tem registros de 32 bytes; versão ou DLC diferente")
        self.players = self._players()
        self.teams = self._teams()
        self.competitions = []
        for i in range(len(self.raw["competitions"]) // 32):
            row = self.raw["competitions"][i * 32:(i + 1) * 32]
            code = row[4:32].split(b"\0", 1)[0].decode("ascii", errors="replace")
            self.competitions.append((i, row[:4].hex(" "), code))

    def _players(self):
        raw = self.raw["players"]
        result = []
        for i in range(len(raw) // PLAYER_RECORD_SIZE):
            base = i * PLAYER_RECORD_SIZE
            player_id = struct.unpack_from("<I", raw, base + PLAYER_ID_OFFSET)[0]
            name = _read_fixed(raw, base + PLAYER_NAME_OFFSET, PLAYER_NAME_SIZE)
            shirt = _read_fixed(raw, base + PLAYER_SHIRT_OFFSET, PLAYER_NAME_SIZE)
            result.append(Player(i, player_id, name, shirt))
        return result

    def _teams(self):
        raw = self.raw["teams"]
        result = []
        for i in range(len(raw) // TEAM_RECORD_SIZE):
            base = i * TEAM_RECORD_SIZE
            team_id = struct.unpack_from("<I", raw, base + 8)[0]
            name = _read_fixed(raw, base + TEAM_ENGLISH_NAME_OFFSET, TEAM_NAME_SIZE)
            result.append(Team(i, team_id, name))
        return result

    def edit_player(self, player_id: int, name: str, shirt_name: str):
        player = next((p for p in self.players if p.player_id == player_id), None)
        if not player:
            raise DatabaseError(f"Não encontrei o jogador ID {player_id}")
        raw = bytearray(self.raw["players"])
        base = player.index * PLAYER_RECORD_SIZE
        _write_fixed(raw, base + PLAYER_NAME_OFFSET, PLAYER_NAME_SIZE, name)
        _write_fixed(raw, base + PLAYER_SHIRT_OFFSET, PLAYER_NAME_SIZE, shirt_name)
        self.raw["players"] = bytes(raw)
        player.name, player.shirt_name = name, shirt_name

    def edit_team(self, team_id: int, name: str):
        team = next((t for t in self.teams if t.team_id == team_id), None)
        if not team:
            raise DatabaseError(f"Não encontrei o time ID {team_id}")
        raw = bytearray(self.raw["teams"])
        base = team.index * TEAM_RECORD_SIZE
        _write_fixed(raw, base + TEAM_ENGLISH_NAME_OFFSET, TEAM_NAME_SIZE, name)
        self.raw["teams"] = bytes(raw)
        team.name = name

    def create_player(self, template_player_id: int, player_id: int, name: str, shirt_name: str, team_id: int, jersey: int = 0):
        if player_id <= 0 or any(p.player_id == player_id for p in self.players):
            raise DatabaseError("O ID do novo jogador precisa ser positivo e ainda não estar em uso")
        template = next((p for p in self.players if p.player_id == template_player_id), None)
        if template is None:
            raise DatabaseError("Selecione um jogador-base válido")
        if not any(t.team_id == team_id for t in self.teams):
            raise DatabaseError("Selecione um time existente")
        if not 0 <= jersey <= 99:
            raise DatabaseError("O número da camisa deve ficar entre 0 e 99")
        record = bytearray(self.raw["players"][template.index * PLAYER_RECORD_SIZE:(template.index + 1) * PLAYER_RECORD_SIZE])
        struct.pack_into("<I", record, PLAYER_ID_OFFSET, player_id)
        _write_fixed(record, PLAYER_NAME_OFFSET, PLAYER_NAME_SIZE, name)
        _write_fixed(record, PLAYER_SHIRT_OFFSET, PLAYER_NAME_SIZE, shirt_name)
        self.raw["players"] += bytes(record)

        assignments = bytearray(self.raw["assignments"])
        existing_ids = [struct.unpack_from("<I", assignments, i)[0] for i in range(0, len(assignments), ASSIGNMENT_RECORD_SIZE)]
        assignment_id = max(existing_ids, default=0) + 1
        assignments.extend(struct.pack("<4I", assignment_id, player_id, team_id, jersey))
        self.raw["assignments"] = bytes(assignments)
        self.players.append(Player(len(self.players), player_id, name, shirt_name))

    def create_team_from_slot(self, template_team_id: int, team_id: int, name: str):
        if team_id <= 0 or any(t.team_id == team_id for t in self.teams):
            raise DatabaseError("O ID do novo time precisa ser positivo e ainda não estar em uso")
        template = next((t for t in self.teams if t.team_id == template_team_id), None)
        if template is None:
            raise DatabaseError("Selecione um time-base válido")
        if team_id > 0xFFFF:
            raise DatabaseError("O PES 2017 usa IDs de time de 16 bits nos vínculos; este ID não é aceito")
        record = bytearray(self.raw["teams"][template.index * TEAM_RECORD_SIZE:(template.index + 1) * TEAM_RECORD_SIZE])
        struct.pack_into("<I", record, 8, team_id)
        _write_fixed(record, TEAM_ENGLISH_NAME_OFFSET, TEAM_NAME_SIZE, name)
        self.raw["teams"] += bytes(record)
        self.teams.append(Team(len(self.teams), team_id, name))

    def write_to_folder(self, folder: Path):
        folder.mkdir(parents=True, exist_ok=True)
        for key, relative in self.PATHS.items():
            path = folder / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(encode_wesys(self.prefixes[key], self.raw[key]))

    def export_csv(self, kind: str, filename: Path):
        if kind == "players":
            rows = ((p.player_id, p.name, p.shirt_name) for p in self.players)
            headers = ("player_id", "name", "shirt_name")
        elif kind == "teams":
            rows = ((t.team_id, t.name) for t in self.teams)
            headers = ("team_id", "name")
        else:
            raise DatabaseError("Tipo CSV desconhecido")
        with filename.open("w", encoding="utf-8-sig", newline="") as f:
            writer = csv.writer(f)
            writer.writerow(headers)
            writer.writerows(rows)

    def import_csv(self, kind: str, filename: Path):
        with filename.open("r", encoding="utf-8-sig", newline="") as f:
            rows = list(csv.DictReader(f))
        if kind == "players":
            for row in rows:
                self.edit_player(int(row["player_id"]), row["name"], row["shirt_name"])
        elif kind == "teams":
            for row in rows:
                self.edit_team(int(row["team_id"]), row["name"])
        else:
            raise DatabaseError("Tipo CSV desconhecido")

