# Banco de dados do jogo

## Estado atual

O editor ainda não identifica com segurança o banco principal de jogadores, times, ligas ou competições. A presença de `Competition.bin` em uma entrada do `dt10_win.cpk` foi comprovada e é coberta por teste de leitura, mas seu schema não foi promovido a parser.

O dashboard deixa as contagens de entidades como não identificadas. O número de CPKs, entradas indexadas, tamanhos e versão do executável só é exibido quando obtido diretamente da instalação.

## Direção

O SQLite é apenas cache/área de trabalho. Os formatos nativos continuarão sendo a fonte de exportação. O modelo deverá aceitar identificadores dinâmicos e separar validade estrutural do BIN de compatibilidade efetiva com uma versão específica de `PES2017.exe`.

