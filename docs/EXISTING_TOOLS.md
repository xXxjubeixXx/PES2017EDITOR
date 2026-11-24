# Ferramentas existentes

As ferramentas foram inventariadas sob `tools/` e usadas como evidência, sem alterar seus arquivos.

| Ferramenta | Uso observado | Decisão |
|---|---|---|
| `tools/CPK TOOL/PES CPK Tool.exe` | Interface de extração/reconstrução CPK | Referência funcional; não automatizada nesta fase |
| `tools/CPK TOOL/CpkMaker.DLL` | API x86 `CriCpkMaker.CpkMaker`, `AnalyzeCpkFile`, `FileData.FileInfos` | Backend futuro isolado; não redistribuir sem licença |
| `tools/Pes 2020 Editor V0.12.10 by Ejogc327` | Editor e utilitários de BIN/CPK | Referência de formatos e limitações; licenças não verificadas |
| `tools/Ghidra` | Engenharia reversa do executável | Não acoplado ao aplicativo |
| `tools/x64dbg` | Debug/instrumentação | Próxima investigação de precedência |
| `tools/rea-rea-agents-6.9.0` | Agentes e apoio de engenharia reversa | Referência local |

O script `tests/diagnostics/phase15_inventory_cpk_members.ps1` documenta os campos retornados pelo helper x86, incluindo `FileId`, `ContentFilePath`, `Filesize`, `Extractsize`, `IsCompressed` e `Offset`. A incompatibilidade x86 e a procedência/licença incerta impedem incorporar a DLL diretamente ao executável x64.

## Dependências NuGet

Os metadados locais dos pacotes usados foram conferidos: `Microsoft.Data.Sqlite` 10.0.0 é MIT; `Microsoft.NET.Test.Sdk` 17.14.1 é MIT; `xunit` 2.9.3 é Apache-2.0; e `SQLitePCLRaw` 2.1.11 é Apache-2.0. A licença não elimina o alerta de segurança NU1903 reportado para `SQLitePCLRaw.lib.e_sqlite3`; a atualização ou substituição deve ocorrer antes da distribuição definitiva.

