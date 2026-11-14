# PES 2017 Editor

Aplicativo simples para Windows que abre os arquivos CPK do PES 2017, extrai seu conteúdo e cria um CPK modificado. A interface usa Tkinter e não exige pacotes Python extras.

## Requisitos

- Windows 10 ou 11
- Python 3.10 ou mais recente, instalado com suporte ao Tkinter
- Uma cópia instalada do PES 2017

## Iniciar

1. Baixe o código deste repositório e extraia a pasta.
2. Execute `iniciar_editor.bat`.
3. Selecione a pasta principal do jogo (a que contém `PES2017.exe`).
4. Escolha um arquivo da lista em `Data`.
5. Extraia o CPK inteiro ou um arquivo individual.
6. Edite os arquivos extraídos com a ferramenta apropriada ao formato (por exemplo, `.bin`, `.ftex` ou `.dds`).
7. Use **Reconstruir CPK…** para criar uma cópia modificada, ou **Instalar no jogo com backup…** para substituir o CPK selecionado.

Antes de substituir um pacote, feche o jogo. O instalador cria uma cópia original em `Backups/PES2017EDITOR` dentro da pasta escolhida do jogo.

## Editor de banco de dados

Abra `dt10_win.cpk` e clique em **Editar jogadores e times…** para:

- alterar nome e nome de camisa de jogadores existentes;
- criar um jogador copiando as características de outro jogador e vinculá-lo a um time;
- alterar o nome em inglês de um time;
- acrescentar um registro de time copiando outro;
- exportar/importar os nomes existentes por CSV;
- consultar a lista de competições do banco.

As alterações são gravadas em um CPK novo. A criação de jogadores e times aumenta os registros de `Player.bin` e `Team.bin`; é experimental e pode não ser aceita pelo jogo. O time criado ainda não é associado a uma liga.

## Limites atuais

- A criação de ligas jogáveis ainda não está implementada. Além do registro de competição, ela depende das regras e da associação dos times. A quantidade de competições e slots também pode depender de limites do executável.
- O editor de jogadores permite nomes e cópia de um modelo; ainda não edita atributos, aparência, posições ou todos os campos de idioma.
- O editor de times altera o nome em inglês. A criação acrescenta um registro, mas ainda não configura nome em todos os idiomas, escudo, uniforme, técnico ou vaga em liga.
- Uniformes, faces e estádios ainda dependem de ferramentas específicas para seus formatos.
- A reconstrução grava os arquivos extraídos sem a compressão CRILAYLA original. O conteúdo permanece no CPK, mas o pacote reconstruído pode ocupar mais espaço.
- A base do jogo usada para mapear os registros tem 12.333 registros de jogadores e 501 registros de times. Acrescentar registros além disso não foi validado em uma sessão real do jogo.
- Alguns CPKs ou variantes com tabelas e compressões incomuns podem não ser compatíveis. Se o arquivo não puder ser lido, o original permanece intacto.
- A reconstrução exige espaço livre suficiente para o CPK de saída. Extrações de pacotes grandes também podem precisar de vários gigabytes.
- Faça uma cópia externa dos dados importantes antes de modificar arquivos de jogo.

## Estrutura do projeto

- `src/pes2017editor/app.py`: interface Tkinter.
- `src/pes2017editor/cpk.py`: leitura, extração e gravação do contêiner CPK/UTF.
- `iniciar_editor.bat`: atalho para iniciar no Windows.

## Créditos

A análise local do jogo usou como referência de formato a documentação e a implementação pública de CPK/UTF do projeto [the4chancup/pes-file-tools](https://github.com/the4chancup/pes-file-tools). O código deste editor é uma implementação independente para este fluxo e não inclui arquivos do jogo.

## Licença

O código original deste projeto está sob a licença MIT. Os arquivos do jogo e os arquivos modificados continuam sujeitos aos direitos dos respectivos titulares.
