from __future__ import annotations

import os
import queue
import shutil
import threading
import time
import tkinter as tk
from pathlib import Path
from tkinter import filedialog, messagebox, ttk

from .cpk import CpkArchive, CpkError
from .database_ui import DatabaseWindow


APP_NAME = "PES 2017 Editor"


def human_size(size):
    value = float(size)
    for unit in ("B", "KB", "MB", "GB", "TB"):
        if value < 1024 or unit == "TB":
            return f"{value:.0f} {unit}" if unit == "B" else f"{value:.1f} {unit}"
        value /= 1024


class Editor(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title(APP_NAME)
        self.geometry("980x660")
        self.minsize(790, 540)
        self.game_dir = tk.StringVar()
        self.filter_text = tk.StringVar()
        self.status = tk.StringVar(value="Escolha a pasta do jogo para começar.")
        self.archive_path: Path | None = None
        self.archive: CpkArchive | None = None
        self.entries = []
        self.messages = queue.Queue()
        self._build_ui()
        self.after(100, self._poll_messages)

    def _build_ui(self):
        outer = ttk.Frame(self, padding=14)
        outer.pack(fill="both", expand=True)
        ttk.Label(outer, text="PES 2017 Editor", font=("Segoe UI", 18, "bold")).pack(anchor="w")
        ttk.Label(outer, text="Abra um CPK, extraia os arquivos, edite-os e gere uma cópia modificada.").pack(anchor="w", pady=(2, 12))

        game_row = ttk.Frame(outer)
        game_row.pack(fill="x")
        ttk.Label(game_row, text="Pasta do jogo:").pack(side="left")
        ttk.Entry(game_row, textvariable=self.game_dir).pack(side="left", fill="x", expand=True, padx=8)
        ttk.Button(game_row, text="Selecionar…", command=self.choose_game).pack(side="left")

        archive_row = ttk.Frame(outer)
        archive_row.pack(fill="x", pady=(10, 8))
        ttk.Label(archive_row, text="Arquivo CPK:").pack(side="left")
        self.archive_box = ttk.Combobox(archive_row, state="readonly", width=35)
        self.archive_box.pack(side="left", padx=8)
        self.archive_box.bind("<<ComboboxSelected>>", self.open_archive)
        ttk.Button(archive_row, text="Abrir outro CPK…", command=self.choose_archive).pack(side="left")
        self.archive_info = ttk.Label(archive_row, text="")
        self.archive_info.pack(side="left", padx=12)

        search_row = ttk.Frame(outer)
        search_row.pack(fill="x", pady=(4, 5))
        ttk.Label(search_row, text="Filtrar arquivos:").pack(side="left")
        search = ttk.Entry(search_row, textvariable=self.filter_text)
        search.pack(side="left", fill="x", expand=True, padx=8)
        self.filter_text.trace_add("write", lambda *_: self.populate_entries())

        cols = ("path", "size", "packed")
        self.tree = ttk.Treeview(outer, columns=cols, show="headings", selectmode="browse")
        self.tree.heading("path", text="Caminho dentro do CPK")
        self.tree.heading("size", text="Tamanho")
        self.tree.heading("packed", text="Armazenado")
        self.tree.column("path", width=620, anchor="w")
        self.tree.column("size", width=100, anchor="e")
        self.tree.column("packed", width=100, anchor="e")
        scrollbar = ttk.Scrollbar(outer, orient="vertical", command=self.tree.yview)
        self.tree.configure(yscrollcommand=scrollbar.set)
        self.tree.pack(side="left", fill="both", expand=True)
        scrollbar.pack(side="right", fill="y")
        self.tree.bind("<Double-1>", lambda _: self.extract_selected_file())

        actions = ttk.Frame(self)
        actions.pack(fill="x", padx=14, pady=(8, 5))
        ttk.Button(actions, text="Extrair CPK inteiro…", command=self.extract_archive).pack(side="left")
        ttk.Button(actions, text="Extrair arquivo selecionado…", command=self.extract_selected_file).pack(side="left", padx=8)
        ttk.Button(actions, text="Reconstruir CPK…", command=self.rebuild_archive).pack(side="left")
        ttk.Button(actions, text="Instalar no jogo com backup…", command=self.install_archive).pack(side="left", padx=8)
        ttk.Button(actions, text="Editar jogadores e times…", command=self.open_database_editor).pack(side="left")
        ttk.Label(self, textvariable=self.status, relief="sunken", anchor="w", padding=(8, 5)).pack(fill="x", side="bottom")

    def choose_game(self):
        chosen = filedialog.askdirectory(title="Selecione a pasta principal do PES 2017")
        if not chosen:
            return
        self.game_dir.set(chosen)
        data_dir = Path(chosen) / "Data"
        cpks = sorted(data_dir.glob("*.cpk"), key=lambda p: p.name.lower()) if data_dir.is_dir() else []
        if not cpks:
            messagebox.showerror(APP_NAME, "Não encontrei arquivos .cpk em uma subpasta Data.")
            return
        self.archive_box["values"] = [p.name for p in cpks]
        self.archive_box.current(0)
        self.open_archive()

    def choose_archive(self):
        chosen = filedialog.askopenfilename(title="Selecione um arquivo CPK", initialdir=self.game_dir.get() or None, filetypes=[("Arquivos CPK", "*.cpk"), ("Todos os arquivos", "*.*")])
        if chosen:
            self._load_archive(Path(chosen))

    def open_archive(self, _event=None):
        if not self.archive_box.get():
            return
        path = Path(self.game_dir.get()) / "Data" / self.archive_box.get()
        self._load_archive(path)

    def _load_archive(self, path):
        try:
            self.archive = CpkArchive(path)
            self.archive_path = path
            self.entries = self.archive.entries
            self.archive_info.configure(text=f"{len(self.entries):,} arquivos • {human_size(path.stat().st_size)}")
            self.populate_entries()
            self.status.set(f"CPK aberto: {path.name}")
        except (OSError, CpkError, KeyError, ValueError) as exc:
            messagebox.showerror(APP_NAME, f"Não consegui abrir o CPK.\n\n{exc}")

    def populate_entries(self):
        if not hasattr(self, "tree"):
            return
        self.tree.delete(*self.tree.get_children())
        query = self.filter_text.get().casefold()
        for index, entry in enumerate(self.entries):
            if query and query not in entry.name.casefold():
                continue
            self.tree.insert("", "end", iid=str(index), values=(entry.name, human_size(entry.size), human_size(entry.packed_size)))

    def _selected_entry(self):
        selection = self.tree.selection()
        if not selection or not self.archive:
            messagebox.showinfo(APP_NAME, "Selecione um arquivo na lista.")
            return None
        return self.entries[int(selection[0])]

    def _run_worker(self, title, callback):
        self.status.set(title)
        self._set_busy(True)
        def run():
            try:
                result = callback()
                self.messages.put(("done", result if isinstance(result, str) else "Operação concluída."))
            except Exception as exc:
                self.messages.put(("error", str(exc)))
        threading.Thread(target=run, daemon=True).start()

    def _set_busy(self, busy):
        for child in self.winfo_children():
            self._busy_children(child, busy)

    def _busy_children(self, widget, busy):
        if isinstance(widget, (ttk.Button, ttk.Combobox)):
            try:
                widget.configure(state="disabled" if busy else ("readonly" if isinstance(widget, ttk.Combobox) else "normal"))
            except tk.TclError:
                pass
        for child in widget.winfo_children():
            self._busy_children(child, busy)

    def _poll_messages(self):
        try:
            kind, msg = self.messages.get_nowait()
            self._set_busy(False)
            self.status.set(msg)
            if kind == "error":
                messagebox.showerror(APP_NAME, msg)
            else:
                messagebox.showinfo(APP_NAME, msg)
        except queue.Empty:
            pass
        self.after(100, self._poll_messages)

    def extract_archive(self):
        if not self.archive:
            messagebox.showinfo(APP_NAME, "Selecione um CPK primeiro.")
            return
        folder = filedialog.askdirectory(title="Selecione a pasta onde extrair o CPK")
        if not folder:
            return
        dest = Path(folder) / f"{self.archive_path.stem}_extraido"
        if dest.exists() and any(dest.iterdir()):
            messagebox.showerror(APP_NAME, f"A pasta já contém arquivos:\n{dest}\n\nEscolha outra pasta para não sobrescrever conteúdo.")
            return
        archive = self.archive
        self._run_worker("Extraindo CPK…", lambda: archive.extract_all(dest, self._progress))

    def open_database_editor(self):
        if not self.archive_path:
            messagebox.showinfo(APP_NAME, "Abra o dt10_win.cpk primeiro.")
            return
        DatabaseWindow(self, self.archive_path)

    def extract_selected_file(self):
        entry = self._selected_entry()
        if not entry:
            return
        target = filedialog.asksaveasfilename(title="Salvar arquivo extraído como", initialfile=Path(entry.name).name)
        if not target:
            return
        archive = self.archive
        def extract():
            Path(target).write_bytes(archive.read(entry))
        self._run_worker("Extraindo arquivo…", extract)

    def rebuild_archive(self):
        if not self.archive_path:
            messagebox.showinfo(APP_NAME, "Selecione um CPK primeiro.")
            return
        folder = filedialog.askdirectory(title="Selecione a pasta extraída e editada")
        if not folder:
            return
        target = filedialog.asksaveasfilename(title="Salvar CPK reconstruído", initialdir=str(self.archive_path.parent), initialfile=f"{self.archive_path.stem}_modificado.cpk", defaultextension=".cpk", filetypes=[("Arquivos CPK", "*.cpk")])
        if not target:
            return
        self._build_to(Path(folder), Path(target))

    def install_archive(self):
        if not self.archive_path:
            messagebox.showinfo(APP_NAME, "Selecione um CPK primeiro.")
            return
        folder = filedialog.askdirectory(title="Selecione a pasta extraída e editada")
        if not folder:
            return
        target = self.archive_path
        if not messagebox.askyesno(APP_NAME, f"O CPK do jogo será substituído:\n\n{target}\n\nUma cópia original será guardada na pasta Backups dentro do jogo. Continuar?"):
            return
        self._build_to(Path(folder), target, install=True)

    def _build_to(self, folder, target, install=False):
        original = self.archive_path
        def build():
            temp_target = target.with_name(target.stem + ".pes2017editor.tmp.cpk") if install else target
            CpkArchive.pack_folder(folder, temp_target, self._progress)
            check = CpkArchive(temp_target)
            if not check.entries:
                temp_target.unlink(missing_ok=True)
                raise CpkError("A validação do CPK reconstruído falhou")
            if install:
                backup_dir = Path(self.game_dir.get()) / "Backups" / "PES2017EDITOR"
                backup_dir.mkdir(parents=True, exist_ok=True)
                backup = backup_dir / f"{original.stem}_{time.strftime('%Y%m%d-%H%M%S')}.cpk"
                shutil.copy2(original, backup)
                os.replace(temp_target, original)
                return f"CPK instalado. Backup original:\n{backup}"
            else:
                return f"CPK reconstruído e validado:\n{target}"
        self._run_worker("Reconstruindo CPK…", build)

    def _progress(self, current, total, name):
        if current == total or current % 50 == 0:
            self.after(0, lambda: self.status.set(f"Processando {current:,}/{total:,}: {name}"))


def main():
    Editor().mainloop()


if __name__ == "__main__":
    main()
