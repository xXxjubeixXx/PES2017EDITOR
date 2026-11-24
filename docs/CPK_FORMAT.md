# CPK, CRI UTF e compressão

## Evidência comprovada

O CPK real analisado começa com `CPK ` e contém uma tabela `@UTF` no cabeçalho a partir de `0x10`. A TOC nativa do `dt10_win.cpk` foi decodificada com 156 entradas. O `dt43_jpn_win.cpk` também possui TOC nativa, com 3 entradas.

Para CRI UTF, a implementação usa inteiros big-endian e a estrutura observada no código de referência `vgmstream/src/coding/cri_utf.c` ([fonte](https://github.com/vgmstream/vgmstream/blob/master/src/coding/cri_utf.c)):

- tamanho da tabela em `+0x04`, relativo ao início da tabela;
- versão em `+0x08`;
- offsets relativos para linhas, strings e dados;
- quantidade de colunas, largura de linha e quantidade de linhas;
- descritores de coluna com flags de nome/default/row e tipo primitivo.

O parser local suporta tipos inteiros, ponto flutuante, string, dados variáveis e preserva os valores lidos como objetos para inspeção.

## TOC e entradas

O leitor procura offsets declarados pelo cabeçalho e assinaturas `TOC `, `ITOC`, `ETOC` e `GTOC` em uma janela inicial limitada. Para uma TOC `@UTF`, usa campos conhecidos por nome como `DirName`, `FileName`, `FileOffset`, `Filesize`, `ExtractSize` e `Codec`, validando caminho e intervalo antes de criar uma entrada.

## Estado parcial

53 dos 55 CPKs da instalação original apresentam `TOC ` detectável, porém o payload não começa por `@UTF` decodificável. Isso é registrado como limitação, não como CPK inválido. A ferramenta x86 existente `CpkMaker.DLL` e os scripts de diagnóstico do projeto pai demonstram uma rota alternativa de leitura, mas ela ainda não foi integrada nem licenciada para redistribuição.

O leitor atual lê bytes armazenados e não promete descompressão completa de todos os codecs CPK. O parser WESYS identifica o invólucro em BINs, mas ainda não expõe a escrita/descompressão de seu payload. Não há reconstrução de CPK na Fase 01.

