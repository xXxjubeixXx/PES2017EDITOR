# Formatos BIN

## Implementado

O núcleo possui um `BinaryParserRegistry` extensível. Quando nenhum parser reconhece o conteúdo, o resultado é `raw`, com tamanho e bytes disponíveis para inspeção hexadecimal. Isso evita descartar campos desconhecidos.

O parser inicial reconhece a assinatura/invólucro `WESYS` em offsets observados nos BINs. Ele mostra o offset da assinatura, o tamanho armazenado e o tamanho descomprimido declarado. Esses campos são fatos do invólucro; a semântica dos dados internos permanece desconhecida.

## Não inventado

Não foram implementados nesta fase parsers para jogadores, times, ligas, competições, estádios, táticas, aparências ou relacionamentos. Existem fichas e relatórios no projeto pai com hipóteses e mapeamentos parciais, mas nenhuma dessas informações foi promovida automaticamente para um modelo de edição.

Cada parser futuro deverá declarar cabeçalho, endianness, encoding, tamanho de registro, campos conhecidos, offsets, flags e bytes desconhecidos. A serialização sem alterações deverá ser comparada byte a byte ao original sempre que o formato estiver suficientemente comprovado.

