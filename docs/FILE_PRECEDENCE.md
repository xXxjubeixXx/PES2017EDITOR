# Precedência e carregamento de arquivos

## O que foi comprovado

O detector localiza `PES2017.exe`, `Data`, `download`, CPKs, candidatos de Data Pack, diretórios de patch, `DpFileList.bin` quando presente e saves `EDIT*` quando encontrados. Localizar um arquivo não é tratado como prova de que o jogo o utiliza.

## O que permanece desconhecido

A ordem efetiva entre CPKs originais, Data Packs, patches, `download`, `DpFileList.bin` e dados de edição não foi comprovada por instrumentação de runtime nesta fase. A interface informa a limitação e não calcula um “arquivo efetivo” apenas pela existência de duplicatas.

## Próxima evidência necessária

Comparar instalações controladas, observar o carregamento com ProcMon ou instrumentação compatível, correlacionar `DpFileList.bin` com os arquivos realmente lidos e validar em cópia experimental do jogo. Essa investigação deve ser feita antes de oferecer aplicação de patch ou geração de CPK adicional.

