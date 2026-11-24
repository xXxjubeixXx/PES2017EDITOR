# PES 2017 EDITOR — Relatório da Fase 03

Data da execução: 24/11/2026. Versão centralizada: `0.3.0`.

## Resultado

A Fase 03 produziu a primeira edição de dados feita pelo próprio editor: o nome de um jogador real é alterado em memória, serializado novamente no envelope WESYS, gravado em uma cópia experimental do `dt10_win.cpk`, reaberto pelo mesmo backend e validado. O CPK original permanece intacto.

A fase não está declarada integralmente concluída. A validação visual dentro do PES, a precedência efetiva de arquivos e a reconstrução geral de CPK ainda estão pendentes.

## Linha de base v0.2.1 → v0.3.0

| Medida | v0.2.1 | v0.3.0 | Evidência |
|---|---:|---:|---|
| CPKs detectados | 55 | 55 | `GameInstallationDetector` + fixture local |
| CPKs com TOC decodificada | 2 | 55 | `CpkArchiveReader`, incluindo TOC mascarada |
| Entradas indexadas | 159 | 40.505 | teste proprietário `IndexesAllRealInstallationCpksWithoutExtractingThem` |
| Jogadores identificados | não suportado | 13.053 | `Player.bin` real, passo 192 |
| Times identificados | não suportado | 521 | `Team.bin` real, passo 1.400 |
| Competições identificadas | não suportado | 62 | `Competition.bin` real, passo 32 |
| Associações lidas | não suportado | 12.977 | `PlayerAssignment.bin` real, passo 16 |
| Campo editável | nenhum | nome do jogador | `ReplacePlayerName` + round-trip |
| CPK experimental | nenhum | cópia layout-preserving | `ExperimentalCpkWriter` |
| Vulnerabilidade SQLite | NU1903 transitivo | não reportada pela auditoria | `dotnet list package --vulnerable` |

Os números são da instalação de referência local e não são apresentados como universais para todas as versões, patches ou Data Packs.

## Investigação CPK

O cabeçalho CPK usa pacote UTF no offset `0x10`. Em CPKs da instalação original, CPK header, TOC, ETOC e GTOC podem conter o pacote UTF mascarado. A rotina pesquisada em `CriPakTools/CriPakTools/CPK.cs` mantém uma chave inteira `m = 0x655f`, aplica `byte ^ (m & 0xff)` e atualiza `m *= 0x4115` em cada byte. Essa lógica foi reproduzida como primitiva interna mínima em `CriUtfCipher.cs`; a DLL `CpkMaker.DLL` não foi incorporada nem redistribuída.

O deslocamento físico das entradas não é simplesmente o valor `FileOffset` da TOC. Na referência local, a base efetiva corresponde à menor base positiva entre `ContentOffset` e `TocOffset`. A camada CPK aplica essa regra e passa aos demais módulos um offset físico já resolvido.

Após a correção:

- `dt10_win.cpk`: 156 entradas;
- `dt43_jpn_win.cpk`: 3 entradas;
- `dt11_win.cpk`: 757 entradas;
- toda a instalação: 55 CPKs, 40.505 entradas, nenhum CPK sem TOC legível;
- os intervalos das entradas são validados contra o tamanho do arquivo antes da indexação.

Uma extração independente do `dt20_bra_win.cpk`, executada localmente com a API x86 existente somente para comparação, produziu 5 arquivos idênticos byte a byte aos 5 arquivos lidos pelo backend gerenciado. Os arquivos proprietários usados nessa comparação não fazem parte do repositório.

## BINs e evidências

O envelope WESYS observado tem 16 bytes, assinatura `WESYS` nos offsets 3–7 para a variante PES local, tamanhos little-endian em 8–11 e 12–15 e payload zlib quando o terceiro byte é `0x81`. A variante direta de `Competition4.bin` é preservada sem tentar aplicar zlib.

Mapeamentos promovidos para o código:

- `Player.bin`: registros de 192 bytes; ID candidato em `+8`; nome UTF-8 fixo em `+144`, capacidade 48; nome de camisa em `+98`, capacidade 46;
- `Team.bin`: registros de 1.400 bytes; ID candidato em `+8`; nome UTF-8 fixo em `+234`, capacidade 70;
- `PlayerAssignment.bin`: registros de 16 bytes; jogador em `+4`, time em `+8`, camisa em `+12`, flags observadas em `+13/+14`;
- `Competition.bin`: registros de 32 bytes; nome observado em `+4`, capacidade 28; o restante permanece não interpretado.

Os campos de nome são classificados como parcialmente comprovados pelos estudos locais; a implementação preserva bytes desconhecidos e somente permite escrita do nome do jogador, campo com round-trip comprovado.

## Fluxo de edição comprovado

1. O editor detecta a instalação e indexa os CPKs.
2. `PesDatabaseReader` lê os quatro BINs reais necessários.
3. O usuário altera `EditedName` de um jogador real.
4. `PesDatabaseParser.ReplacePlayerName` modifica somente a capacidade do campo comprovado.
5. `WesysCodec.Encode` recompõe o envelope.
6. `ExperimentalCpkWriter` copia o CPK para `%LOCALAPPDATA%\\PES2017EDITOR\\projects\\experimental` e substitui apenas o slot existente.
7. O CPK é reaberto e o nome é lido novamente.
8. O CPK de origem é hasheado antes e depois; um backup não sobrescrito é criado em `backups`.

Essa gravação não altera a instalação selecionada e não afirma que o jogo aceitará o arquivo. O teste in-game ainda precisa ser executado em uma cópia isolada.

## Testes

- Build da solução: aprovado, 0 avisos e 0 erros.
- Testes públicos: 8 aprovados, 0 reprovados, 0 ignorados.
- Testes proprietários locais: 8 aprovados, 0 reprovados, 0 ignorados.
- Total: 16 aprovados, 0 reprovados, 0 ignorados.
- Auditoria de pacotes vulneráveis: nenhum pacote vulnerável reportado.
- CPK mascarado independente: 5/5 entradas iguais byte a byte à extração de referência.

O detalhe reproduzível está em `docs/TEST_RESULTS.md` e no projeto `tests/PES2017Editor.Tests`.

## Ferramentas e licenças

Foram consultadas as implementações públicas CriPakTools, CriFsV2Lib, pes-file-tools, unlimited-tool-works, 4ccEditor, pesXdecrypter e cpk-tools. CriPakTools expõe a rotina de UTF mascarada; CriFsV2Lib é útil para CRILAYLA/decrypt callbacks, mas não fornece repack geral. `unlimited-tool-works` declara Apache-2.0/MIT para seu código autoral, separando recursos derivados do jogo. A licença aplicável ao repositório CriPakTools não foi identificada de forma suficiente para redistribuir seu código ou DLL; por isso apenas a transformação mínima foi reproduzida e as referências ficaram documentadas.

## Artefatos e publicação

A infraestrutura de distribuição da Fase 02 foi mantida. O pipeline executou build Release, testes, publish self-contained, Inno Setup, ZIP portátil, hashes e inspeção do staging. Como a validação in-game não foi executada nesta sessão, `v0.3.0` foi publicada corretamente como pré-release experimental; `v0.2.1` continua sendo a última versão estável. A publicação foi confirmada em `https://github.com/xXxjubeixXx/PES2017EDITOR/releases/tag/v0.3.0`, com commit `a7b7ce0aeb0ce1ece8165dcb37da49e50e356fa1`, tag anotada `v0.3.0` e workflow de release aprovado.

Artefatos remotos confirmados:

- instalador: `PES2017DatabaseEditor-Setup-v0.3.0-win-x64.exe`, 68.269.512 bytes, SHA-256 `17f717aaca26fba72cc24b50e0a27010b33a93c97ccc0f898acaafc8749e8052`;
- portátil: `PES2017DatabaseEditor-Portable-v0.3.0-win-x64.zip`, 95.229.984 bytes, SHA-256 `8ea5998bc7a6e64f1a73e8fb694517428d435f283b8deeb2c2725f1715b6632d`;
- `SHA256SUMS.txt` e `update-manifest.json`: presentes e conferidos pela API/endpoint de download;
- o GitHub disponibiliza os arquivos padrão Source code (ZIP) e Source code (TAR.GZ) para a tag.

Teste real do instalador em diretório isolado: instalação código 0, executável iniciado, desinstalação código 0, atalho no Desktop público e Menu Iniciar comum, remoção dos atalhos e diretório confirmada. Migração real sobre uma instalação anterior e atualização automática baixada ainda não foram executadas.

## Próximas etapas

1. Executar o teste manual dentro de uma cópia do PES e registrar o resultado.
2. Implementar validação de payload/codec por entrada e um relatório de compatibilidade por CPK.
3. Implementar reconstrução completa de TOC/ETOC/GTOC e criação de CPK separado.
4. Expandir parsers de jogadores, times, competições e relacionamentos somente com evidência diferencial.
5. Investigar precedência entre Data, download, Data Packs, patches e EDIT.
6. Adicionar inspeção de registros e diagnóstico de expansão sem modificar o projeto paralelo.
