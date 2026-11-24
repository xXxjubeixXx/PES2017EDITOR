# PES 2017 EDITOR — Guia de teste manual da Fase 03

## Preparação

1. Faça uma cópia isolada da instalação do PES 2017. Não use a pasta original como destino de testes.
2. Abra o `PES2017DatabaseEditor.exe`.
3. Selecione a raiz que contém `PES2017.exe`, `Data` e, quando existente, `download`.
4. Aguarde o carregamento terminar.

## Leitura

1. Abra `Arquivos CPK` e confirme que os CPKs são indexados sem extração manual.
2. Confirme a presença de `dt10_win.cpk` e de `common/etc/pesdb/Player.bin`.
3. Abra `Jogadores` e pesquise por ID ou nome.
4. Confirme que o jogador selecionado possui ID e nome reais, sem valores fictícios.
5. Abra `Times` e `Competições` para conferir os registros carregados.

## Edição experimental

1. Escolha um jogador conhecido e anote ID e nome original.
2. Altere o nome para `PES17_EDITOR_TEST`.
3. Clique em `Aplicar`.
4. Confirme o caminho informado em `Projetos experimentais` e o backup em `Backups`.
5. Confirme no status que o CPK experimental foi reaberto e validado.
6. Anote os hashes SHA-256 exibidos nos logs/status.

## Teste dentro do jogo

1. Só em cópia isolada, aplique o CPK experimental conforme a estratégia de teste da instalação.
2. Não substitua o CPK original sem backup manual e sem confirmar a precedência do patch.
3. Abra o jogo e localize o jogador alterado em um menu que carregue o banco correspondente.
4. Registre se o nome apareceu, não apareceu ou se o jogo apresentou erro.
5. Restaure a cópia isolada a partir do backup e confirme que o original do projeto permaneceu igual.

O resultado in-game deve ser registrado como aprovado, reprovado ou inconclusivo. A ausência de teste não autoriza declarar compatibilidade com o jogo.
