# PES 2017 EDITOR — Formatos BIN e parsers da Fase 03

## Envelope WESYS

O envelope observado nos BINs da instalação tem 16 bytes. A assinatura `WESYS` aparece nos bytes 3–7; os tamanhos little-endian ficam nos bytes 8–11 e 12–15. Quando o byte 2 é `0x81`, o payload é zlib; na variante direta, o payload é preservado sem descompressão. `WesysCodec` valida o tamanho declarado e reconstitui o envelope.

## Banco de dados promovido para código

Os passos e offsets abaixo foram confrontados com os BINs extraídos e com os relacionamentos existentes. Eles não representam uma especificação oficial completa.

| BIN | Registro | Campos lidos | Evidência |
|---|---:|---|---|
| `common/etc/pesdb/Player.bin` | 192 bytes | ID `+8`; nome UTF-8 fixo `+144/48`; camisa `+98/46` | 13.053 registros e round-trip do nome |
| `common/etc/pesdb/Team.bin` | 1.400 bytes | ID `+8`; nome UTF-8 fixo `+234/70` | 521 registros, nomes observados |
| `common/etc/pesdb/PlayerAssignment.bin` | 16 bytes | jogador `+4`; time `+8`; camisa `+12`; flags `+13/+14` | 12.977 registros, IDs cruzados |
| `common/etc/pesdb/Competition.bin` | 32 bytes | nome observado `+4/28` | 62 registros, sem escrita habilitada |

Os nomes e IDs são representados como campos parcialmente comprovados. Bytes não mapeados ficam no `RawRecord` e no payload original; o parser não normaliza nem descarta o restante.

## Escrita habilitada

Somente `Player.bin`/nome do jogador está habilitado. `ReplacePlayerName` rejeita valores que não cabem no campo UTF-8 fixo e mantém os demais bytes do payload idênticos. A validação reabre o CPK experimental e confere o nome pelo índice do registro.

## Ainda não promovido

Não são editáveis nesta versão: atributos esportivos, posições, nacionalidade, aparência, ligas, competições, escalações, táticas, descritores visuais, criação de registros e relacionamentos de liga. Nomes de `Team.bin` e `Competition.bin` são exibidos somente para leitura.

## Relacionamentos

O vínculo jogador–time é real quando o ID candidato de `PlayerAssignment.bin` existe simultaneamente em `Player.bin` e `Team.bin`. Na instalação de referência, o snapshot contém 12.977 associações e 12.481 jogadores com um time resolvido; múltiplas associações para o mesmo jogador podem explicar a diferença sem que isso seja tratado como erro.

Relacionamentos time–liga e competição–participante exigem outros BINs e regras do executável; continuam desconhecidos ou parcialmente documentados.

## Metodologia

As estruturas vieram dos estudos locais em `cpks/md/dt10_win.md` e relatórios de engenharia reversa. Uma estrutura só foi usada para escrita quando houve preservação byte a byte fora do campo e round-trip. Hipóteses de outras versões do PES não foram aplicadas automaticamente.
