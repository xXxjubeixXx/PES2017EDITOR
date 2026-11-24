# Achados de engenharia reversa

## Classificação

| Achado | Classificação | Evidência |
|---|---|---|
| CPK começa com `CPK ` | Comprovado | Leitura dos CPKs reais; teste `ReadsNativeCpkHeaderAndTocFromRealDt10` |
| Cabeçalho/TOC podem usar CRI UTF big-endian | Comprovado | CPK real e `cri_utf.c` do vgmstream |
| `dt10_win.cpk` possui 156 entradas nativas | Comprovado | Backend gerenciado e teste real |
| `dt43_jpn_win.cpk` possui 3 entradas nativas | Comprovado | Varredura real de 55 CPKs |
| Maioria das TOCs está mascarada | Parcialmente comprovado | Assinatura `TOC ` encontrada, payload sem `@UTF`; helper x86 é rota alternativa |
| BINs usam envelope WESYS/zlib em famílias observadas | Parcialmente comprovado | Fichas em `cpks/md` e parser inicial; sem afirmar todos os BINs |
| `Competition.bin` é banco completo de competições | Desconhecido | Apenas presença do arquivo foi comprovada |
| Um registro adicional será aceito pelo executável | Hipótese | Não testar nesta fase; executável não é modificado |
| Ordem de precedência Data/patch/download | Desconhecido | Sem prova de runtime |

Na instalação original usada nos testes, `PES2017.exe` reporta versão de arquivo/produto `1.0.0.0`, tamanho `110.180.352` bytes e SHA-256 `2E1B4DF2B94E05276598C4F7B5D9C5594537B185F0BAC5562D46FB93DAACDF7E`. O `dt10_win.cpk` usado nos testes tem `1.073.160` bytes e SHA-256 `1FDBF5E0D9242625C243B719A87FE5FA726ABBFC543058B24C084BB56F64A549`. Esses hashes identificam apenas esta instalação de referência.

## Contradições registradas

Relatórios antigos de hashes e tamanhos do projeto pai descrevem cópias experimentais de `dt10_win.cpk` diferentes do arquivo original atualmente disponível. O hash/tamanho histórico não é usado como verdade global; a próxima fase deve registrar hashes da instalação selecionada e separar original, cópia de teste e artefato reconstruído.

Os relatórios sobre CPK também alternam entre leitura nativa e uso de máscara/ferramenta auxiliar. Isso não é tratado como contradição lógica: são camadas distintas de representação, mas a diferença ainda precisa ser formalizada em um backend reproduzível.

