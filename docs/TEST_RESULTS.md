# PES 2017 EDITOR — Resultados de testes da Fase 03

Data: 24/11/2026.

## Execuções

```text
dotnet build PES2017Editor.sln --no-restore
Compilação: aprovada; 0 avisos; 0 erros

dotnet test PES2017Editor.sln --no-build
Total: 16; aprovados: 16; reprovados: 0; ignorados: 0

dotnet test PES2017Editor.sln --no-build --filter Category!=Proprietary
Total: 8; aprovados: 8; reprovados: 0; ignorados: 0

dotnet list PES2017Editor.sln package --include-transitive --vulnerable
Projetos vulneráveis: nenhum reportado
```

## Cobertura aprovada

- detecção da instalação;
- leitura de cabeçalho e TOC CPK;
- TOC CRI UTF mascarada com a transformação reproduzida;
- ContentOffset/TocOffset de CPKs PES;
- indexação dos 55 CPKs e 40.505 entradas reais;
- extração individual sem alterar o arquivo original;
- WESYS zlib e WESYS direto;
- preservação de bytes desconhecidos ao editar nome;
- leitura de jogadores, times, competições e associações;
- serialização e reabertura de uma cópia experimental do `dt10_win.cpk`;
- cache SQLite temporário;
- cancelamento e persistência de preferências já existentes na infraestrutura.

## Validação independente

O CPK `dt20_bra_win.cpk` foi extraído com a ferramenta x86 local `CpkMaker.DLL`, somente em pasta temporária de comparação. As 5 entradas foram comparadas com `CpkArchiveReader.ReadEntryAsync`: 5/5 hashes/bytes iguais. A DLL e a extração não são distribuídas.

## Não executado ou pendente

- validação visual no jogo: não executada;
- reconstrução completa de CPK e criação de novas entradas: não implementada;
- round-trip de todos os codecs CRILAYLA/WESYS encontrados: não executado;
- instalação limpa, desinstalação e atualização da v0.3.0: não executados nesta fase;
- testes de precedência real e arquivo efetivamente consumido pelo jogo: desconhecidos;
- Authenticode: não configurado.

Os testes proprietários dependem de arquivos locais e nunca devem ser configurados como fixtures públicas no GitHub Actions.
