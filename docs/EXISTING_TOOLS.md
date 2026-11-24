# PES 2017 EDITOR — Ferramentas existentes

As ferramentas foram inventariadas sob `tools/` e usadas como evidência, sem alterar seus arquivos.

| Ferramenta | Uso observado | Decisão |
|---|---|---|
| `tools/CPK TOOL/PES CPK Tool.exe` | Interface de extração/reconstrução CPK | Referência funcional; não automatizada nesta fase |
| `tools/CPK TOOL/CpkMaker.DLL` | API x86 `CriCpkMaker.CpkMaker`, `AnalyzeCpkFile`, `FileData.FileInfos` | Backend futuro isolado; não redistribuir sem licença |
| `tools/Pes 2020 Editor V0.12.10 by Ejogc327` | Editor e utilitários de BIN/CPK | Referência de formatos e limitações; licenças não verificadas |
| `tools/Ghidra` | Engenharia reversa do executável | Não acoplado ao aplicativo |
| `tools/x64dbg` | Debug/instrumentação | Próxima investigação de precedência |
| `tools/rea-rea-agents-6.9.0` | Agentes e apoio de engenharia reversa | Referência local |

## Referências públicas pesquisadas

| Projeto | Resultado | Decisão |
|---|---|---|
| [CriPakTools](https://github.com/esperknight/CriPakTools) | Expõe CPK/CRI UTF e `DecryptUTF`; código antigo, sem licença identificada de forma suficiente para redistribuição | Reproduzir somente a rotina mínima; não incluir DLL/código como dependência |
| [CriFsV2Lib](https://github.com/Sewer56/CriFsV2Lib) | Leitor/extrator e CRILAYLA com callbacks de decriptação; não é repacker geral | Referência para codec futuro, não acoplado |
| [pes-file-tools](https://github.com/the4chancup/pes-file-tools) | Ferramentas relacionadas a formatos PES | Referência de formatos; compatibilidade PES 2017 deve ser comprovada localmente |
| [unlimited-tool-works](https://github.com/the4chancup/unlimited-tool-works) | Código Rust com componentes PES; declara Apache-2.0/MIT para código autoral e separa recursos derivados do jogo | Referência, sem incorporar recursos proprietários |
| [4ccEditor](https://github.com/the4chancup/4ccEditor) e [pesXdecrypter](https://github.com/the4chancup/pesXdecrypter) | Utilitários específicos para investigação PES | Não são dependências do aplicativo |
| [cpk-tools](https://github.com/ConnorKrammer/cpk-tools) | Fork com suporte a reimport/compressão CRILAYLA e correções de tabelas | Não usar como repacker sem validar licença e round-trip PES |

O script `tests/diagnostics/phase15_inventory_cpk_members.ps1` documenta os campos retornados pelo helper x86, incluindo `FileId`, `ContentFilePath`, `Filesize`, `Extractsize`, `IsCompressed` e `Offset`. A incompatibilidade x86 e a procedência/licença incerta impedem incorporar a DLL diretamente ao executável x64.

## Dependências NuGet

Os metadados locais dos pacotes usados foram conferidos: `Microsoft.Data.Sqlite` 10.0.0 é MIT; `Microsoft.NET.Test.Sdk` 17.14.1 é MIT; `xunit` 2.9.3 é Apache-2.0; e `SQLitePCLRaw` é Apache-2.0. `SQLitePCLRaw.lib.e_sqlite3` foi fixado em 2.1.14 para substituir o transitivo vulnerável observado em 2.1.11. A auditoria atual com `dotnet list package --vulnerable` não reporta pacotes vulneráveis.

