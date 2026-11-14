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
        "regulations": "common/etc/pesdb/CompetitionRegulation.bin",
        "entries": "common/etc/pesdb/CompetitionEntry.bin",
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
        if len(self.raw["regulations"]) % 148:
            raise DatabaseError("CompetitionRegulation.bin não tem registros de 148 bytes; versão ou DLC diferente")
        if len(self.raw["entries"]) % 12:
            raise DatabaseError("CompetitionEntry.bin não tem registros de 12 bytes")
        self.players = self._players()
        self.teams = self._teams()
        self.competitions = []
        for i in range(len(self.raw["competitions"]) // 32):
            row = self.raw["competitions"][i * 32:(i + 1) * 32]
            code = row[4:32].split(b"\0", 1)[0].decode("ascii", errors="replace")
            self.competitions.append((i, row[:4].hex(" "), code))

    @staticmethod
    def _strings(raw, start, end):
        result = []
        cursor = start
        while cursor < end:
            nul = raw.find(b"\0", cursor, end)
            if nul < 0:
                break
            if nul > cursor:
                try:
                    text = raw[cursor:nul].decode("ascii")
                except UnicodeDecodeError:
                    text = ""
                if text:
                    result.append((cursor, nul + 1, text))
            cursor = nul + 1
        return result

    def league_templates(self):
        regulations = self.raw["regulations"]
        templates = []
        for i in range(len(regulations) // 148):
            row = regulations[i * 148:(i + 1) * 148]
            if row[6] != 4:
                continue
            code = row[16:48].split(b"\0", 1)[0].decode("ascii", errors="replace")
            if code:
                templates.append((code, i, row[7]))
        return templates

    def create_league(self, name: str, team_count: int, template_team_id: int, template_code: str, players_per_team: int = 23):
        if not name.strip() or len(name.encode("utf-8")) > 90:
            raise DatabaseError("O nome da liga deve conter de 1 a 90 bytes UTF-8")
        if not 2 <= team_count <= 63:
            raise DatabaseError("A liga aceita de 2 a 63 times neste formato")
        if not 11 <= players_per_team <= 40:
            raise DatabaseError("Cada elenco deve ter de 11 a 40 jogadores")
        if any(t.name.casefold() == name.casefold() for t in self.teams):
            raise DatabaseError("Já existe um time com esse nome; escolha outro nome para a liga")
        if not any(t.team_id == template_team_id for t in self.teams):
            raise DatabaseError("O time-base não existe no banco")

        reg_template = next((r for r in self.league_templates() if r[0] == template_code), None)
        if reg_template is None:
            raise DatabaseError("A competição-base precisa ser uma liga com regras compatíveis")
        _, reg_index, old_cid = reg_template
        comp_index = next((i for i, _, code in self.competitions if code == template_code), None)
        if comp_index is None:
            raise DatabaseError("Não encontrei o código da liga-base em Competition.bin")

        used_cids = {self.raw["regulations"][i * 148 + 7] for i in range(len(self.raw["regulations"]) // 148)}
        cid = next((candidate for candidate in range(max(used_cids, default=0) + 1, 256) if candidate not in used_cids), None)
        if cid is None:
            raise DatabaseError("Não há IDs de competição livres de 8 bits no banco")
        used_regids = {struct.unpack_from("<H", self.raw["regulations"], i * 148 + 2)[0] for i in range(len(self.raw["regulations"]) // 148)}
        reg_id = next((candidate for candidate in range(max(used_regids, default=0) + 1, 0x10000) if candidate not in used_regids), None)
        if reg_id is None:
            raise DatabaseError("Não há IDs de regra livres no banco")
        code = f"CUSTOM_{cid:03d}"

        # Cria os novos clubes copiando um registro válido, com IDs únicos.
        team_raw = bytearray(self.raw["teams"])
        source_team = next(t for t in self.teams if t.team_id == template_team_id)
        team_template = team_raw[source_team.index * TEAM_RECORD_SIZE:(source_team.index + 1) * TEAM_RECORD_SIZE]
        next_team_id = max(t.team_id for t in self.teams) + 1
        new_team_ids = []
        made_teams = []
        for i in range(team_count):
            team_id = next_team_id + i
            if team_id > 0xFFFFFFFF:
                raise DatabaseError("Os IDs de time excedem o campo do banco")
            club_name = f"{name} {i + 1:02d}"
            record = bytearray(team_template)
            struct.pack_into("<I", record, 8, team_id)
            _write_fixed(record, TEAM_ENGLISH_NAME_OFFSET, TEAM_NAME_SIZE, club_name)
            team_raw.extend(record)
            new_team_ids.append(team_id)
            made_teams.append(Team(len(self.teams) + i, team_id, club_name))

        # Copia um elenco existente preservando posições, atributos e numeração.
        player_raw = bytearray(self.raw["players"])
        assignment_raw = bytearray(self.raw["assignments"])
        roster = []
        for offset in range(0, len(assignment_raw), ASSIGNMENT_RECORD_SIZE):
            row = assignment_raw[offset:offset + ASSIGNMENT_RECORD_SIZE]
            if struct.unpack_from("<I", row, 8)[0] == template_team_id:
                roster.append(row)
        roster.sort(key=lambda row: (struct.unpack_from("<I", row, 12)[0] >> 8, struct.unpack_from("<I", row, 12)[0] & 0xFF))
        player_by_id = {p.player_id: p for p in self.players}
        player_indices = {p.player_id: p.index for p in self.players}
        roster = [row for row in roster if struct.unpack_from("<I", row, 4)[0] in player_indices]
        if len(roster) < players_per_team:
            raise DatabaseError(f"O time-base só tem {len(roster)} jogadores válidos; são necessários {players_per_team}")
        roster = roster[:players_per_team]
        next_player_id = max(player_by_id) + 1
        next_assignment_id = max(struct.unpack_from("<I", assignment_raw, i)[0] for i in range(0, len(assignment_raw), ASSIGNMENT_RECORD_SIZE)) + 1
        for team_index, team_id in enumerate(new_team_ids):
            club_name = made_teams[team_index].name
            for player_index, source_assignment in enumerate(roster):
                source_id = struct.unpack_from("<I", source_assignment, 4)[0]
                source_player = player_by_id[source_id]
                record_start = source_player.index * PLAYER_RECORD_SIZE
                record = bytearray(player_raw[record_start:record_start + PLAYER_RECORD_SIZE])
                player_id = next_player_id
                next_player_id += 1
                if player_id > 0xFFFFFFFF:
                    raise DatabaseError("Os IDs de jogador excedem o campo do banco")
                struct.pack_into("<I", record, PLAYER_ID_OFFSET, player_id)
                _write_fixed(record, PLAYER_NAME_OFFSET, PLAYER_NAME_SIZE, f"{club_name} Player {player_index + 1:02d}")
                _write_fixed(record, PLAYER_SHIRT_OFFSET, PLAYER_NAME_SIZE, f"{club_name} {player_index + 1:02d}")
                player_raw.extend(record)
                assignment = bytearray(source_assignment)
                struct.pack_into("<I", assignment, 0, next_assignment_id)
                struct.pack_into("<I", assignment, 4, player_id)
                struct.pack_into("<I", assignment, 8, team_id)
                assignment_raw.extend(assignment)
                next_assignment_id += 1

        # Regulation rows in PES 2017 are 148 bytes; these offsets come from the
        # game's own England D1 row, whose regulation id, type, competition id,
        # and team count correspond to its CompetitionEntry records.
        reg_data = bytearray(self.raw["regulations"])
        reg = bytearray(reg_data[reg_index * 148:(reg_index + 1) * 148])
        reg[0:2] = bytes(2)  # no relegation destination
        struct.pack_into("<H", reg, 2, reg_id)
        reg[6] = 4
        reg[7] = cid
        reg[11] = team_count
        for start, _old_end, _text in self._strings(reg, 16, 148):
            if start == 16:
                value = code
                end = 48
            elif start == 48:
                value = name
                end = 148
            else:
                continue
            encoded = value.encode("utf-8")
            if len(encoded) >= end - start:
                raise DatabaseError("O nome interno ou visível da liga excede o campo disponível")
            reg[start:end] = encoded + bytes(end - start - len(encoded))
        reg_data.extend(reg)

        comp_data = bytearray(self.raw["competitions"])
        comp = bytearray(comp_data[comp_index * 32:(comp_index + 1) * 32])
        encoded_code = code.encode("ascii")
        if len(encoded_code) >= 28:
            raise DatabaseError("O código da liga excede 27 caracteres")
        comp[2:4] = struct.pack("<H", ((cid - 1) // 2) | (struct.unpack_from("<H", comp, 2)[0] & 0xFF00))
        comp[4:32] = encoded_code + bytes(28 - len(encoded_code))
        comp_data.extend(comp)

        entry_data = bytearray(self.raw["entries"])
        next_entry_id = max((struct.unpack_from("<I", entry_data, i + 4)[0] >> 16
                             for i in range(0, len(entry_data), 12)), default=0) + 1
        for slot, team_id in enumerate(new_team_ids, 1):
            entry_data.extend(struct.pack("<IIHH", team_id, (next_entry_id + slot - 1) << 16, slot << 8, cid))

        self.raw["teams"] = bytes(team_raw)
        self.raw["players"] = bytes(player_raw)
        self.raw["assignments"] = bytes(assignment_raw)
        self.raw["regulations"] = bytes(reg_data)
        self.raw["competitions"] = bytes(comp_data)
        self.raw["entries"] = bytes(entry_data)
        self.teams.extend(made_teams)
        self.players = self._players()
        self.competitions.append((len(self.competitions), "custom", code))
        return {"competition_id": cid, "regulation_id": reg_id, "team_ids": new_team_ids, "players_created": team_count * players_per_team}

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

