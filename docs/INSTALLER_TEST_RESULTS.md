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

O instalador `v0.2.0` foi compilado em `artifacts/installer`. O ZIP portátil e os hashes são gerados por `scripts/release.ps1`; a publicação oficial só deve ocorrer depois da validação do conteúdo desses artefatos.

Artefatos publicados e confirmados pela API do GitHub:

- instalador: 68.180.065 bytes, SHA-256 `1BC8158240C551567AA4FB7588D09EF386E03410E26346AB8FFFBE180B01B6CA`;
- portátil: 95.091.817 bytes, SHA-256 `7B9BCDD11DFD6AA6FCA669B1C67C010E5AE96C27294D849AE439758F757C472E`;
- manifesto: `update-manifest.json`, 967 bytes.

Ainda não foi testada uma migração sobre uma instalação produtiva anterior nem uma atualização real baixada de uma release pública; esses cenários exigem uma release de teste/controlada e estão descritos como próximos testes.

