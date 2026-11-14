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

## Limites da versão 0.1.0

- Esta versão trabalha com o contêiner CPK; ela não edita diretamente os formatos internos de times, jogadores, uniformes, faces ou estádios. Esses arquivos precisam de editores adequados ao seu formato.
- A reconstrução grava os arquivos extraídos sem a compressão CRILAYLA original. O conteúdo permanece no CPK, mas o pacote reconstruído pode ocupar mais espaço.
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
