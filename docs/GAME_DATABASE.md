# PES 2017 EDITOR — Banco de dados do jogo

## Estado da Fase 03

O banco principal usado pela instalação de referência foi localizado em `common/etc/pesdb` dentro do `dt10_win.cpk`. O editor lê, por meio do CPK e WESYS, `Player.bin`, `Team.bin`, `Competition.bin` e `PlayerAssignment.bin`.

Contagens observadas:

- 13.053 jogadores;
- 521 times;
- 62 competições;
- 12.977 associações em `PlayerAssignment.bin`;
- 12.481 jogadores com um time resolvido no mapa de relacionamentos.

Essas contagens são fatos da instalação de referência, não limites universais do PES 2017.

## Campos usados

Os campos de ID, nome e relacionamento estão documentados em `BINARY_FORMATS.md`. O parser preserva bytes desconhecidos. O único campo editável é o nome do jogador, com capacidade UTF-8 fixa comprovada por round-trip.

Nacionalidade, idade, altura, atributos, aparência, ligas, competições, treinadores, escalações e táticas ainda não são promovidos a campos editáveis. A presença de arquivos com esses nomes não é suficiente para afirmar o schema.

## SQLite

O SQLite é somente cache/área de trabalho para índices e preferências. Os BINs e CPKs nativos continuam sendo a fonte de exportação. O modelo separa validade estrutural do BIN de compatibilidade efetiva com uma versão do `PES2017.exe`.
