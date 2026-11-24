# PES 2017 EDITOR — Changelog

## 0.3.0 — Fase 03, motor de dados experimental

- Decodificação interna de TOCs CRI UTF mascaradas com a transformação `DecryptUTF` documentada.
- Correção do deslocamento físico PES usando a menor base positiva entre `ContentOffset` e `TocOffset`.
- Cobertura da instalação de referência ampliada de 159 para 40.505 entradas em 55 CPKs.
- Leitura real de Player, Team, Competition e PlayerAssignment.
- Editor de jogadores com pesquisa, lista virtualizada e nome editável.
- Geração, reabertura e validação de cópia experimental layout-preserving.
- Backups SHA-256 não sobrescritos antes da edição experimental.
- Teste independente de 5/5 entradas de `dt20_bra_win.cpk`.
- Atualização do pacote SQLite e auditoria sem vulnerabilidades reportadas.
- Documentação de evidências, limitações e resultado da Fase 03.

## 0.2.1 — correção de empacotamento

- inclusão do módulo CPK ausente na árvore publicada;
- separação entre testes públicos e testes que exigem fixtures proprietárias;
- validação manual do workflow de release sem sobrescrever `v0.2.0`.

## 0.2.0 — Fase 02

- versionamento SemVer centralizado;
- instalador Inno Setup e helper de atualização;
- dados locais em `%LOCALAPPDATA%\\PES2017EDITOR`;
- verificação de releases estáveis com SHA-256;
- scripts PowerShell e workflows CI/CD;
- distribuição portátil self-contained Windows x64.

## 0.1.0 — Fase 01

- solução modular .NET 10/WPF/MVVM;
- detecção factual, leitura CPK/CRI UTF nativa, indexação e cache SQLite;
- extração individual e parser raw/WESYS inicial;
- interface escura e testes xUnit contra CPKs reais.
