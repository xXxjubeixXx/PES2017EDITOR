# Relatório da Fase 01

## 1. Resumo

Foi criada uma primeira versão executável do PES 2017 Database Editor, isolada em `PES2017EDITOR`. O aplicativo detecta uma instalação, indexa CPKs sem extraí-los integralmente, exibe fatos reais, permite explorar entradas e extrair um arquivo individual para outro destino. O modo é deliberadamente somente leitura.

## 2. Pesquisa realizada

Foi feito inventário e leitura dos 191 Markdown na raiz do projeto pai e dos 57 Markdown em `cpks/md`, além da leitura detalhada dos relatórios relevantes de CPK, ferramentas, hashes, validação, formatos BIN, fluxo de expansão e engenharia reversa. Também foram analisados scripts de diagnóstico e ferramentas em `tools/`, incluindo a API observada do `CpkMaker.DLL` x86.

## 3. Descobertas

- A instalação original contém 55 CPKs em `originals/Pro Evolution Soccer 2017/Data`.
- `dt10_win.cpk` tem CPK/CRI UTF/TOC nativos e 156 entradas.
- `dt43_jpn_win.cpk` tem TOC nativa e 3 entradas.
- Os 53 restantes têm TOC detectável, mas não decodificável pelo backend atual; são marcados como parciais.
- `Competition.bin` foi localizado e extraído em memória para teste, mas não é interpretado.
- A precedência efetiva dos arquivos não foi comprovada.

A referência usada tem `PES2017.exe` `1.0.0.0`, SHA-256 `2E1B4DF2B94E05276598C4F7B5D9C5594537B185F0BAC5562D46FB93DAACDF7E`; os hashes são identificadores da cópia testada, não uma assinatura universal de todas as instalações.

## 4. Arquitetura

Adotou-se .NET 10, C#, WPF, MVVM, SQLite e xUnit. O núcleo é separado da UI. O contrato `ICpkArchiveBackend` permite integrar futuramente o helper x86 ou outra biblioteca sem reescrever o aplicativo.

## 5. Funcionalidades implementadas

- seleção de pasta e detecção de instalação;
- histórico de instalações recentes;
- progresso assíncrono e cancelamento;
- identificação de CPKs;
- indexação virtual e duplicatas;
- leitura CRI UTF nativa;
- filtro e tabela virtualizada;
- extração individual com `CreateNew`;
- inspeção hexadecimal/raw e WESYS;
- dashboard sem contagens inventadas;
- cache SQLite WAL;
- documentação permanente e testes reais.

## 6. Arquivos criados

Código em `src/`, testes em `tests/`, documentação em `docs/` e solução `PES2017Editor.sln`. O único diretório de artefatos destinado à distribuição é `artifacts/publish/win-x64` após publicação.

## 7. Resultados

Build Release aprovado. Testes xUnit aprovados: 6/6. A varredura real processou 55 CPKs e indexou 159 entradas. A comparação antes/depois do teste de extração confirmou que o `dt10_win.cpk` não foi alterado.

O executável self-contained Windows x64 está em `artifacts/publish/win-x64/PES2017DatabaseEditor.exe`, com 142.022.352 bytes e SHA-256 `BD40784B0948057610D451025BE0A6A67A05882E1E291608A0B8744F263B36D9`. O smoke test confirmou que o processo inicia e permanece ativo.

## 8. Limitações e bloqueios

TOCs mascaradas, codecs, WESYS interno, schemas BIN, precedência, edição e reconstrução ainda exigem pesquisa. A DLL x86 existente possui procedência/licença de redistribuição não esclarecida. O pacote SQLite apresenta aviso transitivo NU1903.

## 9. Preservação

Nenhum arquivo original do jogo foi escrito. Nenhum arquivo do projeto de expansão foi escrito. A extração de teste usa memória e destino temporário separado; a aplicação usa arquivos de destino novos.

## 10. Próxima fase

Priorizar backend x86 isolado ou decodificação gerenciada das TOCs mascaradas, descompressão validada, parsers BIN com round-trip e investigação controlada da precedência.

