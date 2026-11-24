# Changelog

## 0.2.1 — correção de empacotamento

- inclusão do módulo CPK ausente na árvore publicada;
- separação entre testes públicos e testes que exigem fixtures proprietárias;
- validação manual do workflow de release sem sobrescrever `v0.2.0`.

## 0.2.0 — Fase 02

- Centralizado o versionamento SemVer em `Directory.Build.props`.
- Adicionados metadados de versão na interface e propriedades do executável.
- Criados ícone original, instalador Inno Setup e helper de atualização.
- Separados dados locais em `%LOCALAPPDATA%\PES2017EDITOR`.
- Adicionada verificação de releases estáveis do GitHub com validação de SHA-256.
- Adicionados scripts PowerShell de build, teste, publicação e release.
- Adicionados workflows CI/CD e artefatos portátil/instalador.
- Testada instalação, criação de atalhos, execução e desinstalação isoladas.

## 0.1.0 — Fase 01

- Criada solução modular .NET 10 com WPF/MVVM.
- Criada detecção factual de instalações PES 2017.
- Criada leitura de cabeçalho CPK e CRI UTF nativa.
- Criada indexação assíncrona, duplicatas e cache SQLite WAL.
- Criada extração individual sem modificar o CPK de origem.
- Criado parser raw e inspeção inicial de envelopes WESYS.
- Criada interface escura com dashboard, explorador, progresso e navegação futura.
- Criados testes xUnit contra CPKs reais e cache local.
- Documentadas limitações de TOC mascarada, codecs, precedência e licenciamento.

