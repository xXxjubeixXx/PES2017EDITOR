# Relatório da Fase 02

Após a publicação inicial, a correção `v0.2.1` foi publicada para incluir o projeto CPK que havia sido ocultado por um padrão excessivo do `.gitignore`. A `v0.2.0` foi preservada sem force-push; a `v0.2.1` é a versão recomendada.

## Estado

Implementação concluída para versionamento, distribuição Windows, instalador e atualizador. A release pública foi criada e verificada remotamente.

## Repositório

O remoto verificado é `https://github.com/xXxjubeixXx/PES2017EDITOR`, público, branch principal `main`. A autenticação GitHub CLI está disponível. O conteúdo anterior estava no commit `7eef9445fd4177963a1c996b0cf0a1997e8dcff2` e foi preservado na branch remota `archive/pre-phase-02-20261124` antes da substituição.

O commit publicado da implementação é `1df85a4f13a008da9f363c345f4c630809508a8c`, com o commit antigo como pai. A correção está no commit `72a7d90fcc1716de77908f9c31003b5086c7cc28`, tag `v0.2.1`, disponível em [github.com/xXxjubeixXx/PES2017EDITOR/releases/tag/v0.2.1](https://github.com/xXxjubeixXx/PES2017EDITOR/releases/tag/v0.2.1).

## Implementação

- SemVer centralizado em `Directory.Build.props` com versão `0.2.1`;
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

O teste automatizado local tem 9 casos aprovados e o CI público tem 4 casos públicos aprovados. Na release `v0.2.1`, o instalador tem 68.188.344 bytes e SHA-256 `6CB3F506ECA85265D71C079593CC374B1A1B7B0AA8B04C914D13C82521ED42EB`; o ZIP portátil tem 95.092.388 bytes e SHA-256 `0DD0D36E78F10A2413B1E105840C8187B0190CB356850FBE01C4A1B9AC2B4B8C`. Esses valores estão no manifesto e em `SHA256SUMS.txt` da release.

## Segurança e limitações

Nenhum token GitHub, certificado privado ou arquivo proprietário do jogo foi incorporado. SHA-256 valida integridade, mas não substitui assinatura Authenticode. O pacote SQLite ainda gera NU1903 transitivo. A consulta ao GitHub falha de forma não bloqueante quando não há internet.

## Próximos passos

O commit, a tag, a release e o manifesto foram verificados remotamente. Próximo passo: baixar a release em uma máquina de teste separada e executar uma atualização real sobre uma instalação anterior, sem usar a instalação do jogo como ambiente de teste.

