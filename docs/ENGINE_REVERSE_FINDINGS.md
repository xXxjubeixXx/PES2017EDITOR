# PES 2017 EDITOR — Achados de engenharia reversa

## Classificação

| Achado | Classificação | Evidência |
|---|---|---|
| CPK começa com `CPK ` | Comprovado | Leitura dos CPKs reais; teste `ReadsNativeCpkHeaderAndTocFromRealDt10` |
| Cabeçalho/TOC podem usar CRI UTF big-endian | Comprovado | CPK real e `cri_utf.c` do vgmstream |
| `dt10_win.cpk` possui 156 entradas nativas | Comprovado | Backend gerenciado e teste real |
| `dt43_jpn_win.cpk` possui 3 entradas nativas | Comprovado | Varredura real de 55 CPKs |
| TOCs mascaradas podem ser decodificadas por `DecryptUTF` | Comprovado na instalação de referência | Rotina pública pesquisada e reimplementação interna; 55/55 CPKs indexados |
| `FileOffset` exige base física PES | Comprovado na instalação de referência | `min(ContentOffset, TocOffset)` reproduz a extração independente |
| BINs usam envelope WESYS/zlib em famílias observadas | Parcialmente comprovado | Fichas em `cpks/md`, leitura real e parser; sem afirmar todos os BINs |
| `Player.bin`, `Team.bin`, `Competition.bin` e `PlayerAssignment.bin` são bancos interpretáveis | Parcialmente comprovado | Passos, campos candidatos e relacionamentos cruzados na instalação local |
| `Competition.bin` contém 62 registros no banco de referência | Comprovado para a cópia analisada | Parser real e teste proprietário |
| Um registro adicional será aceito pelo executável | Hipótese | Não testar nesta fase; executável não é modificado |
| Ordem de precedência Data/patch/download | Desconhecido | Sem prova de runtime |

Na instalação original usada nos testes, `PES2017.exe` reporta versão de arquivo/produto `1.0.0.0`, tamanho `110.180.352` bytes e SHA-256 `2E1B4DF2B94E05276598C4F7B5D9C5594537B185F0BAC5562D46FB93DAACDF7E`. O `dt10_win.cpk` usado nos testes tem `1.073.160` bytes e SHA-256 `1FDBF5E0D9242625C243B719A87FE5FA726ABBFC543058B24C084BB56F64A549`. Esses hashes identificam apenas esta instalação de referência.

## Contradições registradas

Relatórios antigos de hashes e tamanhos do projeto pai descrevem cópias experimentais de `dt10_win.cpk` diferentes do arquivo original atualmente disponível. O hash/tamanho histórico não é usado como verdade global; a próxima fase deve registrar hashes da instalação selecionada e separar original, cópia de teste e artefato reconstruído.

Os relatórios sobre CPK alternam entre leitura nativa e uso de máscara/ferramenta auxiliar. A Fase 03 formalizou a camada como pacote UTF mascarado e deslocamento físico relativo; a reconstrução geral ainda precisa de validação própria.

