# Testes do instalador

Data: 24/11/2026.

## Hotfix v0.3.1

- instalador compilado com Inno Setup 6.7.3;
- instalação sobre a v0.3.0: código 0;
- registro Windows atualizado para `0.3.1`;
- executável instalado iniciou diretamente;
- atalho Desktop público iniciou a janela;
- atalho Menu Iniciar comum iniciou a janela;
- `--safe-mode` iniciou a janela;
- janela validada como visível, não minimizada, responsiva e 1440×900;
- screenshot real capturado via `PrintWindow`.

## Instalação isolada

- instalador Inno Setup 6.7.3 compilado com sucesso;
- destino temporário fora de `Program Files`;
- código de saída da instalação: `0`;
- executável e helper presentes;
- desinstalador presente;
- atalho criado no Desktop público;
- atalho criado no Menu Iniciar comum;
- aplicativo instalado iniciou e permaneceu ativo durante o smoke test;
- desinstalador executado silenciosamente com código `0`;
- log confirmou remoção dos arquivos, atalhos, registro e diretório de instalação;
- `%LOCALAPPDATA%\PES2017EDITOR` não foi incluído no diretório de instalação e não é removido pelo script Inno.

## Artefatos v0.3.0 publicados

O instalador `v0.3.0` foi compilado com Inno Setup 6.7.3, publicado pelo workflow oficial e validado pela API do GitHub. O ZIP portátil e os hashes são gerados por `scripts/release.ps1`.

Artefatos publicados e confirmados:

- instalador `v0.3.0`: 68.269.512 bytes, SHA-256 `17f717aaca26fba72cc24b50e0a27010b33a93c97ccc0f898acaafc8749e8052`;
- portátil `v0.3.0`: 95.229.984 bytes, SHA-256 `8ea5998bc7a6e64f1a73e8fb694517428d435f283b8deeb2c2725f1715b6632d`;
- `SHA256SUMS.txt`: 231 bytes;
- `update-manifest.json`: 973 bytes, canal `experimental`;
- URL: `https://github.com/xXxjubeixXx/PES2017EDITOR/releases/tag/v0.3.0`.

Ainda não foi testada uma migração sobre uma instalação produtiva anterior nem uma atualização real baixada de uma release pública; esses cenários exigem uma release de teste/controlada e estão descritos como próximos testes.

