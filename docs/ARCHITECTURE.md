# PES 2017 EDITOR — Arquitetura

## Camadas

```text
WPF/MVVM (App)
        ↓ contratos e fatos
Core ─ Game ─ Cpk ─ Binary
        ↓ persistência auxiliar
Database (SQLite) ─ Infrastructure (hash, recentes)
```

- `Core`: modelos agnósticos de UI, progresso, compatibilidade e problemas.
- `Game`: detecta `PES2017.exe`, `Data`, `download`, CPKs, `DpFileList.bin`, patches e saves sem assumir que um arquivo encontrado é efetivamente carregado.
- `Cpk`: lê cabeçalho, CRI UTF direto/mascarado, TOC/ETOC/GTOC detectáveis, resolve offsets PES, indexa sob demanda e extrai uma entrada por streaming.
- `Binary`: WESYS/zlib, WESYS direto e registro extensível de parsers; dados desconhecidos permanecem preservados.
- `Database`: parsers comprovados de Player, Team, Competition e PlayerAssignment; SQLite é cache WAL e não substitui os arquivos do jogo.
- `Infrastructure`: hash SHA-256, instalações recentes, projetos, backups e atualização.
- `App`: navegação, progresso, filtros e comandos; não contém lógica de parsing.

## Backend CPK

`ICpkArchiveBackend` é a fronteira de substituição. `CpkArchiveReader` é o backend gerenciado atual. Um backend futuro poderá chamar um processo x86 para bibliotecas antigas, retornando os mesmos modelos. A decisão de usar um helper deve considerar licenciamento, IPC, cancelamento e isolamento de falhas.

## Fluxo de carga

1. O usuário escolhe uma pasta.
2. O detector cria um `InstallationInfo` factual.
3. O indexador abre cada CPK, sem extrair o contêiner inteiro.
4. O resultado é persistido no SQLite local e exibido no dashboard.
5. A leitura do conteúdo ocorre somente quando uma entrada é selecionada.

## Escrita experimental atual

O nome do jogador passa por `Database` → `WesysCodec` → `ExperimentalCpkWriter`. O writer copia o CPK para uma pasta experimental, grava o slot físico e reabre o resultado antes de marcá-lo como validado. O caminho original nunca é destino.

## Escrita futura

A gravação deverá ser um pipeline separado: alterações pendentes → validação → hash/backup → staging → reconstrução → reabertura e comparação → aplicação controlada. Nenhum serviço de escrita foi conectado nesta fase.

