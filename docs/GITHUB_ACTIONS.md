# GitHub Actions

## CI

`.github/workflows/ci.yml` executa em `windows-latest` para push na `main`, pull requests e execução manual. Ele instala o SDK .NET 10, restaura, compila Release, executa os testes públicos (`Category!=Proprietary`) e verifica vulnerabilidades NuGet. Os cinco testes dependentes de arquivos proprietários do PES são marcados com a categoria `Proprietary` e executados somente em uma cópia local autorizada do jogo; nenhum arquivo do jogo é enviado ao GitHub.

## Release

`.github/workflows/release.yml` é acionado por tags `v*.*.*` ou manualmente. Ele instala Inno Setup, executa `scripts/release.ps1 -PrepareOnly -PublicOnly` e, quando acionado por uma tag, publica os quatro artefatos obrigatórios usando `GITHUB_TOKEN` com a permissão mínima `contents: write`. A execução manual serve para validar a preparação sem tentar substituir uma release existente.

O workflow não contém credenciais privadas. A vulnerabilidade transitiva conhecida do SQLite é reportada no CI, mas permanece uma limitação explícita até ser resolvida.

