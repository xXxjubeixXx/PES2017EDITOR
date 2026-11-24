# PES 2017 EDITOR — CPK, CRI UTF e conteúdo PES

## Estado comprovado

O backend lê os CPKs reais da instalação local, começando por `CPK `. O pacote UTF do cabeçalho começa normalmente em `0x10`. CPKs da instalação também podem mascarar o pacote UTF do cabeçalho e das tabelas.

O formato CRI UTF usa cabeçalho e descritores big-endian. Os números do envelope CPK que delimitam o pacote UTF são little-endian. A tabela contém colunas por nome, incluindo `DirName`, `FileName`, `FileOffset`, `Filesize`, `ExtractSize` e `Codec`.

## UTF mascarado

Quando o pacote não começa em `@UTF`, o editor aplica a transformação observada em `CriPakTools/CriPakTools/CPK.cs`:

```text
m = 0x0000655f
t = 0x00004115
para cada byte d:
    saída = d XOR (m AND 0xff)
    m = m * t em aritmética de 32 bits
```

O código está em `src/PES2017Editor.Cpk/CriUtfCipher.cs`. A implementação externa não é uma dependência do produto e `CpkMaker.DLL` não é distribuída.

## Offsets

O `FileOffset` encontrado na TOC é relativo à base efetiva da área do CPK no formato PES observado. A base usada pelo leitor é a menor entre os valores positivos `ContentOffset` e `TocOffset`. O offset físico retornado às camadas superiores é:

```text
physicalOffset = FileOffset + min(ContentOffset, TocOffset)
```

Quando o cabeçalho não é decodificável, a base é zero e a entrada é tratada como não verificada. Na instalação de referência o cabeçalho e as TOCs foram decodificados para os 55 CPKs.

## Compatibilidade observada na instalação de referência

| Arquivo | Entradas |
|---|---:|
| `dt10_win.cpk` | 156 |
| `dt43_jpn_win.cpk` | 3 |
| `dt11_win.cpk` | 757 |
| Todos os 55 CPKs | 40.505 |

Cada linha é validada por caminho, offset físico, tamanho armazenado e limite do contêiner. Isso comprova indexação e limites, não a interpretação de todos os payloads internos.

## Conteúdo e compressão

O leitor entrega bytes armazenados sem extrair todos os arquivos para disco. A extração individual usa streaming. O envelope WESYS dos BINs é interpretado pelo módulo `Binary`: zlib quando o terceiro byte é `0x81` e payload direto para a variante sem compressão. Isso é compressão interna do BIN, distinta do campo `Codec` do CPK.

CRILAYLA e codecs adicionais ainda não estão integrados ao leitor principal; CriFsV2Lib foi estudada como referência para uma futura camada de codec, sem assumir que seus callbacks de outro jogo sejam compatíveis com PES 2017.

## Escrita experimental

`ExperimentalCpkWriter` cria uma cópia do CPK e substitui um slot existente quando o novo conteúdo cabe no tamanho armazenado. A TOC, os demais slots e o arquivo original não são reescritos. O restante do slot é zerado e não é considerado conteúdo lógico depois do tamanho declarado no WESYS.

Isso não é ainda uma reconstrução geral: não cria entradas, não recalcula TOC/ETOC/GTOC e não deve ser aplicado diretamente à instalação do usuário.

## Fontes

- Documentação local `cpks/md/dt10_win.md` e relatórios da Fase 22 do projeto pai;
- código local `CpkArchiveReader`, `CriUtfTable` e `CriUtfCipher`;
- CriPakTools, consultado em [esperknight/CriPakTools](https://github.com/esperknight/CriPakTools);
- CriFsV2Lib, consultado em [Sewer56/CriFsV2Lib](https://github.com/Sewer56/CriFsV2Lib).

Os arquivos originais e extrações proprietárias não são parte deste repositório.
