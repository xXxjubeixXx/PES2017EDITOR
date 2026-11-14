from __future__ import annotations

import queue
import tempfile
import threading
from pathlib import Path
from tkinter import filedialog, messagebox, simpledialog, ttk

import tkinter as tk

from .cpk import CpkArchive
from .database import DatabaseError, DatabaseWorkspace


class DatabaseWindow(tk.Toplevel):
    def __init__(self, master, cpk_path: Path):
        super().__init__(master)
        self.title("Jogadores e times — banco do jogo")
        self.geometry("930x610")
        self.minsize(760, 490)
        self.source = cpk_path
        self.messages = queue.Queue()
        try:
            self.db = DatabaseWorkspace(cpk_path)
        except (DatabaseError, OSError, KeyError, ValueError) as exc:
            messagebox.showerror("Banco do jogo", str(exc), parent=self)
            self.destroy()
            return
        self._build()
        self.after(100, self._poll)

    def _build(self):
        body = ttk.Frame(self, padding=12)
        body.pack(fill="both", expand=True)
        ttk.Label(body, text=f"Banco carregado de {self.source.name}", font=("Segoe UI", 12, "bold")).pack(anchor="w")
        ttk.Label(body, text="Os nomes são editados na tabela. Os botões Criar acrescentam registros experimentais ao banco.").pack(anchor="w", pady=(2, 8))
        self.tabs = ttk.Notebook(body)
        self.tabs.pack(fill="both", expand=True)
        self.player_tab = ttk.Frame(self.tabs, padding=6)
        self.team_tab = ttk.Frame(self.tabs, padding=6)
        self.competition_tab = ttk.Frame(self.tabs, padding=6)
        self.tabs.add(self.player_tab, text="Jogadores")
        self.tabs.add(self.team_tab, text="Times")
        self.tabs.add(self.competition_tab, text="Competições")
        self.player_tree = self._table(self.player_tab, ("id", "name", "shirt"), ("ID", "Nome", "Nome na camisa"), (90, 350, 260))
        self.team_tree = self._table(self.team_tab, ("id", "name"), ("ID", "Time"), (120, 520))
        self.competition_tree = self._table(self.competition_tab, ("row", "flags", "code"), ("Registro", "Dados", "Código da competição"), (90, 140, 500))
        for row, flags, code in self.db.competitions:
            self.competition_tree.insert("", "end", values=(row, flags, code))
        ttk.Label(self.competition_tab, text="As competições são apenas exibidas nesta versão. Para criar uma liga jogável, o editor ainda precisa alterar também as regras e os vínculos dos times.").pack(anchor="w", pady=(6, 0))
        self.player_tree.bind("<Double-1>", lambda _e: self.edit_player())
        self.team_tree.bind("<Double-1>", lambda _e: self.edit_team())
        self.player_filter = tk.StringVar()
        self.team_filter = tk.StringVar()
        for tab, var, placeholder in ((self.player_tab, self.player_filter, "Buscar jogador"), (self.team_tab, self.team_filter, "Buscar time")):
            row = ttk.Frame(tab)
            row.pack(fill="x", pady=(0, 6))
            ttk.Label(row, text="Filtro:").pack(side="left")
            ttk.Entry(row, textvariable=var).pack(side="left", fill="x", expand=True, padx=6)
        self.player_filter.trace_add("write", lambda *_: self.refresh_players())
        self.team_filter.trace_add("write", lambda *_: self.refresh_teams())
        self.refresh_players()
        self.refresh_teams()

        actions = ttk.Frame(body)
        actions.pack(fill="x", pady=(8, 0))
        ttk.Button(actions, text="Editar jogador selecionado", command=self.edit_player).pack(side="left")
        ttk.Button(actions, text="Criar jogador por cópia…", command=self.create_player).pack(side="left", padx=5)
        ttk.Button(actions, text="CSV jogadores…", command=self.csv_players).pack(side="left", padx=5)
        ttk.Button(actions, text="Editar time selecionado", command=self.edit_team).pack(side="left", padx=(18, 5))
        ttk.Button(actions, text="Criar time por cópia…", command=self.create_team).pack(side="left", padx=5)
        ttk.Button(actions, text="CSV times…", command=self.csv_teams).pack(side="left", padx=5)
        bottom = ttk.Frame(body)
        bottom.pack(fill="x", pady=(10, 0))
        ttk.Button(bottom, text="Salvar banco em um CPK novo…", command=self.save_copy).pack(side="right")
        self.status = tk.StringVar(value="Clique duas vezes em um item para editar seu nome.")
        ttk.Label(body, textvariable=self.status, relief="sunken", anchor="w", padding=(6, 4)).pack(fill="x", pady=(8, 0))

    @staticmethod
    def _table(parent, columns, headings, widths):
        frame = ttk.Frame(parent)
        frame.pack(fill="both", expand=True)
        tree = ttk.Treeview(frame, columns=columns, show="headings", selectmode="browse")
        for col, heading, width in zip(columns, headings, widths):
            tree.heading(col, text=heading)
            tree.column(col, width=width, anchor="w" if col != "id" else "e")
        scroll = ttk.Scrollbar(frame, orient="vertical", command=tree.yview)
        tree.configure(yscrollcommand=scroll.set)
        tree.pack(side="left", fill="both", expand=True)
        scroll.pack(side="right", fill="y")
        return tree

    def refresh_players(self):
        if not hasattr(self, "player_tree"):
            return
        self.player_tree.delete(*self.player_tree.get_children())
        query = self.player_filter.get().casefold()
        for p in self.db.players:
            if query and query not in f"{p.player_id} {p.name} {p.shirt_name}".casefold():
                continue
            self.player_tree.insert("", "end", iid=str(p.player_id), values=(p.player_id, p.name, p.shirt_name))

    def refresh_teams(self):
        if not hasattr(self, "team_tree"):
            return
        self.team_tree.delete(*self.team_tree.get_children())
        query = self.team_filter.get().casefold()
        for t in self.db.teams:
            if query and query not in f"{t.team_id} {t.name}".casefold():
                continue
            self.team_tree.insert("", "end", iid=str(t.team_id), values=(t.team_id, t.name))

    def edit_player(self):
        sel = self.player_tree.selection()
        if not sel:
            return
        player = next(p for p in self.db.players if p.player_id == int(sel[0]))
        name = simpledialog.askstring("Editar jogador", "Nome:", initialvalue=player.name, parent=self)
        if name is None:
            return
        shirt = simpledialog.askstring("Editar jogador", "Nome na camisa:", initialvalue=player.shirt_name, parent=self)
        if shirt is None:
            return
        try:
            self.db.edit_player(player.player_id, name, shirt)
            self.refresh_players()
            self.player_tree.selection_set(str(player.player_id))
            self.status.set(f"Alterado: {player.name}")
        except DatabaseError as exc:
            messagebox.showerror("Editar jogador", str(exc), parent=self)

    def edit_team(self):
        sel = self.team_tree.selection()
        if not sel:
            return
        team = next(t for t in self.db.teams if t.team_id == int(sel[0]))
        name = simpledialog.askstring("Editar time", "Nome em inglês:", initialvalue=team.name, parent=self)
        if name is None:
            return
        try:
            self.db.edit_team(team.team_id, name)
            self.refresh_teams()
            self.team_tree.selection_set(str(team.team_id))
            self.status.set(f"Alterado: {team.name}")
        except DatabaseError as exc:
            messagebox.showerror("Editar time", str(exc), parent=self)

    def create_player(self):
        if not self.db.players:
            return
        source = self._choose_id("Copiar dados de qual jogador ID?", [p.player_id for p in self.db.players], self.db.players[0].player_id)
        if source is None:
            return
        player_id = simpledialog.askinteger("Novo jogador", "Novo ID (ID maior que o atual máximo costuma ser usado):", initialvalue=max(p.player_id for p in self.db.players) + 1, minvalue=1, maxvalue=0xFFFFFFFF, parent=self)
        if player_id is None:
            return
        name = simpledialog.askstring("Novo jogador", "Nome:", parent=self)
        if name is None:
            return
        shirt = simpledialog.askstring("Novo jogador", "Nome na camisa:", initialvalue=name.upper(), parent=self)
        if shirt is None:
            return
        team_id = self._choose_id("Adicionar à equipe (ID do time)?", [t.team_id for t in self.db.teams], self.db.teams[0].team_id)
        if team_id is None:
            return
        jersey = simpledialog.askinteger("Novo jogador", "Número da camisa:", initialvalue=1, minvalue=0, maxvalue=99, parent=self)
        if jersey is None:
            return
        try:
            self.db.create_player(source, player_id, name, shirt, team_id, jersey)
            self.refresh_players()
            self.player_tree.selection_set(str(player_id))
            self.status.set(f"Jogador {name} adicionado. A inclusão de registros é experimental.")
        except DatabaseError as exc:
            messagebox.showerror("Criar jogador", str(exc), parent=self)

    def create_team(self):
        if not self.db.teams:
            return
        source = self._choose_id("Copiar dados de qual time ID?", [t.team_id for t in self.db.teams], self.db.teams[0].team_id)
        if source is None:
            return
        team_id = simpledialog.askinteger("Novo time", "Novo ID de time:", initialvalue=max(t.team_id for t in self.db.teams) + 1, minvalue=1, maxvalue=0xFFFF, parent=self)
        if team_id is None:
            return
        name = simpledialog.askstring("Novo time", "Nome em inglês:", parent=self)
        if name is None:
            return
        try:
            self.db.create_team_from_slot(source, team_id, name)
            self.refresh_teams()
            self.team_tree.selection_set(str(team_id))
            messagebox.showwarning("Time experimental criado", "O registro foi acrescentado a Team.bin, mas não foi vinculado a uma liga. O jogo pode ignorá-lo ou recusar o banco; ainda estamos mapeando as tabelas de competição.", parent=self)
        except DatabaseError as exc:
            messagebox.showerror("Criar time", str(exc), parent=self)

    def _choose_id(self, prompt, values, initial):
        val = simpledialog.askinteger("Selecionar registro", prompt, initialvalue=initial, minvalue=1, parent=self)
        if val is None:
            return None
        if val not in values:
            messagebox.showerror("Selecionar registro", "Esse ID não existe no banco carregado.", parent=self)
            return None
        return val

    def csv_players(self):
        self._csv("players")

    def csv_teams(self):
        self._csv("teams")

    def _csv(self, kind):
        action = messagebox.askyesnocancel("CSV", "Sim: exportar o banco atual para CSV.\nNão: importar um CSV editado.\nCancelar: voltar.", parent=self)
        if action is None:
            return
        if action:
            export = filedialog.asksaveasfilename(parent=self, title="Exportar CSV", defaultextension=".csv", filetypes=[("CSV", "*.csv")])
            if not export:
                return
            try:
                self.db.export_csv(kind, Path(export))
                self.status.set(f"CSV exportado: {export}")
            except (OSError, DatabaseError) as exc:
                messagebox.showerror("CSV", str(exc), parent=self)
            return
        source = filedialog.askopenfilename(parent=self, title="Importar CSV", filetypes=[("CSV", "*.csv")])
        if source:
            try:
                self.db.import_csv(kind, Path(source))
                self.refresh_players() if kind == "players" else self.refresh_teams()
                self.status.set(f"CSV importado. Confira e salve em um CPK novo.")
            except (OSError, KeyError, ValueError, DatabaseError) as exc:
                messagebox.showerror("CSV", str(exc), parent=self)

    def save_copy(self):
        target = filedialog.asksaveasfilename(parent=self, title="Salvar banco modificado como CPK", initialdir=str(self.source.parent), initialfile=f"{self.source.stem}_banco_editado.cpk", defaultextension=".cpk", filetypes=[("Arquivos CPK", "*.cpk")])
        if not target:
            return
        target = Path(target)
        if target.resolve() == self.source.resolve():
            messagebox.showerror("Salvar CPK", "Escolha um nome de saída. O original não é sobrescrito nesta tela.", parent=self)
            return
        if any(t.index >= 501 for t in self.db.teams) or any(p.index >= 12333 for p in self.db.players):
            if not messagebox.askyesno("Confirmar extensão experimental", "Este banco acrescenta registros além das quantidades do PES 2017 original. A compatibilidade do jogo ainda não está confirmada. Salvar uma cópia experimental?", parent=self):
                return
        self.status.set("Gerando CPK com o banco atualizado…")
        def worker():
            try:
                with tempfile.TemporaryDirectory(prefix="pes2017editor-db-") as temp:
                    folder = Path(temp) / "cpk"
                    self.db.archive.extract_all(folder)
                    self.db.write_to_folder(folder)
                    CpkArchive.pack_folder(folder, target)
                check = CpkArchive(target)
                self.messages.put(("ok", f"CPK salvo. {len(check.entries)} arquivos no pacote.\n{target}"))
            except Exception as exc:
                self.messages.put(("error", str(exc)))
        threading.Thread(target=worker, daemon=True).start()

    def _poll(self):
        try:
            kind, text = self.messages.get_nowait()
            self.status.set(text)
            if kind == "ok":
                messagebox.showinfo("Banco do jogo", text, parent=self)
            else:
                messagebox.showerror("Banco do jogo", text, parent=self)
        except queue.Empty:
            pass
        self.after(100, self._poll)
