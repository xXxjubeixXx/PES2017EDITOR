# PES 2017 EDITOR — Estado do projeto

Última atualização: 24/11/2026. Versão centralizada: `0.3.0`.

## Estado atual

Fase 03 em implementação avançada. O núcleo lê os CPKs mascarados da instalação de referência, carrega jogadores, times, competições e associações reais e gera uma primeira cópia experimental com nome de jogador alterado. A versão ainda é experimental: não há confirmação dentro do jogo nem reconstrução geral de CPK.

## Funcionalidades implementadas e verificadas

- solução modular .NET 10/WPF/MVVM;
- detecção da instalação e instalações recentes;
- CPK/CRI UTF direto e mascarado com `DecryptUTF`;
- base física efetiva `min(ContentOffset, TocOffset)`;
- 55 CPKs e 40.505 entradas indexadas na instalação local;
- extração individual sob demanda;
- WESYS/zlib e WESYS direto;
- `Player.bin`, `Team.bin`, `Competition.bin` e `PlayerAssignment.bin` reais;
- 13.053 jogadores, 521 times, 62 competições e 12.977 associações;
- filtro instantâneo e DataGrid virtualizado para jogadores;
- edição do nome do jogador em cópia experimental;
- backup SHA-256, reabertura, validação e preservação do CPK original;
- correção da dependência transitiva SQLite vulnerável;
- infraestrutura de distribuição v0.2.1 preservada.

## Evidências

- `tests/PES2017Editor.Tests/Phase03DataTests.cs` contém round-trip sintético, leitura real e geração/reabertura de CPK;
- `tests/PES2017Editor.Tests/WorkspaceTests.cs` valida 55 CPKs e 40.505 entradas;
- comparação independente do `dt20_bra_win.cpk`: 5/5 arquivos iguais byte a byte;
- `dotnet list PES2017Editor.sln package --include-transitive --vulnerable`: nenhum pacote vulnerável reportado;
- hashes dos arquivos originais foram preservados antes da edição experimental.

## Problemas pendentes

- validação manual dentro do PES 2017;
- precedência efetiva entre Data, download, Data Packs, patches e EDIT;
- reconstrução geral de TOC/ETOC/GTOC e criação de novas entradas;
- CRILAYLA e outros codecs ainda não integrados ao backend principal;
- campos de atributos, ligas, competições, aparências e táticas;
- diagnóstico relacional completo e inspetor diferencial de registros;
- testes isolados de instalação/atualização para a nova versão;
- Authenticode e assinatura de manifesto.

## Decisões arquiteturais

O leitor CPK resolve detalhes de contêiner; o módulo Binary resolve WESYS; Database interpreta somente campos comprovados; App usa MVVM e não conhece offsets. O SQLite é cache auxiliar. A escrita atual é explicitamente layout-preserving e isolada em `%LOCALAPPDATA%\\PES2017EDITOR\\projects\\experimental`.

`CpkMaker.DLL` não é redistribuída: a licença/procedência não autoriza incorporá-la. O algoritmo mínimo de UTF mascarado foi reproduzido internamente após inspeção da implementação pública de referência.

## Comandos reproduzíveis

```powershell
& 'C:\Program Files\dotnet\dotnet.exe' restore .\PES2017Editor.sln
& 'C:\Program Files\dotnet\dotnet.exe' build .\PES2017Editor.sln -c Release --no-restore
& 'C:\Program Files\dotnet\dotnet.exe' test .\PES2017Editor.sln -c Release --no-restore
& 'C:\Program Files\dotnet\dotnet.exe' list .\PES2017Editor.sln package --include-transitive --vulnerable
```

## Últimos resultados

- Build Debug: 0 avisos, 0 erros.
- Testes completos: 16 aprovados, 0 reprovados, 0 ignorados.
- Testes públicos: 8 aprovados.
- Testes proprietários: 8 aprovados.
- Release estável anterior: `v0.2.1`, [GitHub](https://github.com/xXxjubeixXx/PES2017EDITOR/releases/tag/v0.2.1).
- Publicação v0.3.0: pré-release experimental confirmada no GitHub; `v0.2.1` continua sendo a última release estável.
- Artefatos remotos confirmados: instalador, portátil, `SHA256SUMS.txt`, `update-manifest.json` e código-fonte automático ZIP/TAR.GZ.
- CI do commit `a7b7ce0` e workflow de release `45503904505`: aprovados.

## Próximo passo seguro

Executar a edição de um jogador em cópia isolada do jogo, registrar CPK/BIN/ID/nome/hashes e decidir quando a validação in-game permitirá promover a versão experimental a release estável.
