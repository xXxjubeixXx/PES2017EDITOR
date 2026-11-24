# Arquitetura

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
- `Cpk`: lê o cabeçalho CPK e tabelas CRI UTF quando disponíveis, indexa sob demanda e extrai uma entrada por streaming.
- `Binary`: registro extensível de parsers; dados desconhecidos permanecem no caminho raw.
- `Database`: cache WAL de índices; não substitui os arquivos do jogo.
- `Infrastructure`: hash SHA-256 e instalações recentes.
- `App`: navegação, progresso, filtros e comandos; não contém lógica de parsing.

## Backend CPK

`ICpkArchiveBackend` é a fronteira de substituição. `CpkArchiveReader` é o backend gerenciado atual. Um backend futuro poderá chamar um processo x86 para bibliotecas antigas, retornando os mesmos modelos. A decisão de usar um helper deve considerar licenciamento, IPC, cancelamento e isolamento de falhas.

## Fluxo de carga

1. O usuário escolhe uma pasta.
2. O detector cria um `InstallationInfo` factual.
3. O indexador abre cada CPK, sem extrair o contêiner inteiro.
4. O resultado é persistido no SQLite local e exibido no dashboard.
5. A leitura do conteúdo ocorre somente quando uma entrada é selecionada.

## Escrita futura

A gravação deverá ser um pipeline separado: alterações pendentes → validação → hash/backup → staging → reconstrução → reabertura e comparação → aplicação controlada. Nenhum serviço de escrita foi conectado nesta fase.

