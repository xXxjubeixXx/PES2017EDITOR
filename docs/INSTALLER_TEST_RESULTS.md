# Testes do instalador

Data: 24/11/2026.

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

## Artefatos locais observados

O instalador `v0.2.1` foi compilado em `artifacts/installer` e validado pela release workflow. O ZIP portátil e os hashes são gerados por `scripts/release.ps1`.

Artefatos publicados e confirmados pela API do GitHub:

- instalador `v0.2.1`: 68.188.344 bytes, SHA-256 `6CB3F506ECA85265D71C079593CC374B1A1B7B0AA8B04C914D13C82521ED42EB`;
- portátil `v0.2.1`: 95.092.388 bytes, SHA-256 `0DD0D36E78F10A2413B1E105840C8187B0190CB356850FBE01C4A1B9AC2B4B8C`;
- manifesto: `update-manifest.json`, 967 bytes.

Ainda não foi testada uma migração sobre uma instalação produtiva anterior nem uma atualização real baixada de uma release pública; esses cenários exigem uma release de teste/controlada e estão descritos como próximos testes.

