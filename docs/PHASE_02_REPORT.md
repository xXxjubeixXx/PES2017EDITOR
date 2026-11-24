# Relatório da Fase 02

## Estado

Implementação concluída para versionamento, distribuição Windows, instalador e atualizador. A release pública foi criada e verificada remotamente.

## Repositório

O remoto verificado é `https://github.com/xXxjubeixXx/PES2017EDITOR`, público, branch principal `main`. A autenticação GitHub CLI está disponível. O conteúdo anterior estava no commit `7eef9445fd4177963a1c996b0cf0a1997e8dcff2` e foi preservado na branch remota `archive/pre-phase-02-20261124` antes da substituição.

O commit publicado da implementação é `1df85a4f13a008da9f363c345f4c630809508a8c`, com o commit antigo como pai. A tag `v0.2.0` e a release oficial estão disponíveis em [github.com/xXxjubeixXx/PES2017EDITOR/releases/tag/v0.2.0](https://github.com/xXxjubeixXx/PES2017EDITOR/releases/tag/v0.2.0).

## Implementação

- SemVer centralizado em `Directory.Build.props` com versão `0.2.0`;
- versão sincronizada no assembly, UI, User-Agent, instalador, nomes de artefatos e scripts;
- ícone original em SVG, PNG e ICO, sem logotipo Konami;
- instalador Inno Setup em português brasileiro;
- atalhos, AppId permanente e desinstalação;
- dados locais separados em `%LOCALAPPDATA%\PES2017EDITOR`;
- serviço GitHub HTTPS com SemVer, releases estáveis, manifesto, progresso e SHA-256;
- helper de atualização que aguarda o processo principal;
- scripts reproduzíveis de build, teste, publicação, instalador e release;
- workflows CI e release com permissões mínimas;
- testes SemVer, persistência de configurações, instalação isolada e smoke test.

## Verificações

Build e testes locais foram executados após a implementação. A solução e os testes dependem da mesma instalação real usada na Fase 01; no CI público, fixtures proprietárias ausentes são ignoradas explicitamente. O instalador foi instalado em diretório temporário, executado, teve seus atalhos conferidos e foi desinstalado com log.

O teste automatizado atual tem 9 casos aprovados. O instalador `PES2017DatabaseEditor-Setup-v0.2.0-win-x64.exe` tem 68.180.065 bytes e SHA-256 `1BC8158240C551567AA4FB7588D09EF386E03410E26346AB8FFFBE180B01B6CA`. O ZIP portátil tem 95.091.817 bytes e SHA-256 `7B9BCDD11DFD6AA6FCA669B1C67C010E5AE96C27294D849AE439758F757C472E`. Esses valores também estão em `artifacts/release/0.2.0`.

## Segurança e limitações

Nenhum token GitHub, certificado privado ou arquivo proprietário do jogo foi incorporado. SHA-256 valida integridade, mas não substitui assinatura Authenticode. O pacote SQLite ainda gera NU1903 transitivo. A consulta ao GitHub falha de forma não bloqueante quando não há internet.

## Próximos passos

O commit, a tag, a release e o manifesto foram verificados remotamente. Próximo passo: baixar a release em uma máquina de teste separada e executar uma atualização real sobre uma instalação anterior, sem usar a instalação do jogo como ambiente de teste.

