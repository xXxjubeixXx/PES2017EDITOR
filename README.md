# PES 2017 Database Editor

![PES 2017 Database Editor](assets/PES2017DatabaseEditor.png)

Editor desktop independente para investigação, inspeção e futura edição dos dados do Pro Evolution Soccer 2017 para Windows. O projeto não possui vínculo oficial com a Konami.

[![CI](https://github.com/xXxjubeixXx/PES2017EDITOR/actions/workflows/ci.yml/badge.svg)](https://github.com/xXxjubeixXx/PES2017EDITOR/actions/workflows/ci.yml)

## Versão atual

`0.2.1` — correção de empacotamento e publicação da Fase 02.

Baixe a [última release estável](https://github.com/xXxjubeixXx/PES2017EDITOR/releases/latest):

- instalador recomendado: `PES2017DatabaseEditor-Setup-v0.2.1-win-x64.exe`;
- versão portátil: `PES2017DatabaseEditor-Portable-v0.2.1-win-x64.zip`.

Cada release acompanha `SHA256SUMS.txt` e `update-manifest.json`. A assinatura Authenticode ainda não está disponível; hash SHA-256 verifica integridade, mas não autentica sozinho a identidade do publicador.

## Funcionalidades

- detecção da instalação do PES 2017;
- indexação virtual de CPKs e leitura CRI UTF nativa;
- explorador, pesquisa, duplicatas e extração individual;
- preview hexadecimal e parser inicial WESYS;
- dashboard baseado em fatos reais;
- cache SQLite e dados do usuário em `%LOCALAPPDATA%\PES2017EDITOR`;
- interface escura em português brasileiro;
- verificação de releases estáveis pelo GitHub;
- download validado por tamanho e SHA-256;
- atualizador auxiliar que fecha o editor antes de iniciar o instalador;
- instalador Inno Setup com atalhos e desinstalação;
- pacote portátil self-contained Windows x64.

As funcionalidades de edição estruturada, descompressão completa, reconstrução CPK e aplicação de patches continuam explicitamente limitadas até haver evidência e testes suficientes.

## Requisitos

Para usuários: Windows 10/11 x64. O instalador e a versão portátil são self-contained e não exigem SDK, runtime .NET, Python, Java, SQLite separado ou ferramentas externas de CPK.

Para desenvolvimento: Windows x64, .NET SDK 10.x, PowerShell 7 ou Windows PowerShell compatível, Git e Inno Setup 6 para gerar o instalador. Os testes que usam CPKs reais são opcionais quando os arquivos proprietários do PES não estão disponíveis.

## Compilar e testar

```powershell
pwsh .\scripts\build.ps1
pwsh .\scripts\test.ps1
pwsh .\scripts\publish.ps1
pwsh .\scripts\build-installer.ps1
```

O processo prefere o SDK x64 em `%ProgramFiles%\dotnet` e não depende de caminhos do computador do mantenedor. Os artefatos locais ficam em `artifacts/` e não são enviados ao Git.

## Dados e segurança

O editor não altera automaticamente a instalação do PES 2017. Configurações, recentes, cache, logs, projetos e atualizações baixadas ficam em `%LOCALAPPDATA%\PES2017EDITOR`; o instalador fica em `Program Files` e não armazena dados modificáveis do usuário ali.

O `.gitignore` bloqueia executáveis/CPKs/BINs do jogo, saves, backups, credenciais, certificados e artefatos locais. Nenhuma DLL proprietária do CPK Tool é redistribuída.

## Arquitetura

O código é separado em `Core`, `Cpk`, `Binary`, `Database`, `Game`, `Infrastructure`, `App` e `Updater`. O contrato `ICpkArchiveBackend` mantém a UI desacoplada de bibliotecas nativas. Consulte [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).

## Atualizações

O aplicativo consulta de forma assíncrona `https://api.github.com/repos/xXxjubeixXx/PES2017EDITOR/releases/latest`, apenas para releases estáveis. A consulta pode ser desativada em Configurações. Nenhum token é embutido. O instalador baixado só é aceito após validar origem HTTPS permitida, tamanho e SHA-256; atualização e execução exigem ação do usuário.

## Documentação

- [`docs/PHASE_02_REPORT.md`](docs/PHASE_02_REPORT.md)
- [`docs/INSTALLER.md`](docs/INSTALLER.md)
- [`docs/UPDATE_SYSTEM.md`](docs/UPDATE_SYSTEM.md)
- [`docs/VERSIONING.md`](docs/VERSIONING.md)
- [`docs/RELEASE_PROCESS.md`](docs/RELEASE_PROCESS.md)
- [`docs/GITHUB_ACTIONS.md`](docs/GITHUB_ACTIONS.md)
- [`docs/DISTRIBUTION.md`](docs/DISTRIBUTION.md)
- [`docs/INSTALLER_TEST_RESULTS.md`](docs/INSTALLER_TEST_RESULTS.md)
- [`docs/KNOWN_LIMITATIONS.md`](docs/KNOWN_LIMITATIONS.md)

O licenciamento do código autoral permanece pendente de decisão do proprietário do projeto. As licenças das dependências estão documentadas separadamente.
