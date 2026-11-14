# Histórico de alterações

## 0.3.0-alpha.1 — 2026-11-14

- Adicionado um gerador experimental de liga no banco do jogo.
- O gerador duplica regras de uma liga-base, cria times, replica elencos e adiciona vínculos nas tabelas de competição.
- O resultado é salvo somente em um CPK novo e não foi validado em uma sessão real do PES 2017.
- Adicionada instalação do banco no `dt10_win.cpk` com confirmação e backup automático.

## 0.2.0-alpha.1 — 2026-11-14

- Adicionada janela de edição do banco WESYS dentro de `dt10_win.cpk`.
- Lista, pesquisa e edição dos nomes de jogadores e times.
- Criação experimental de jogador por cópia, com vínculo a um time e número de camisa.
- Criação experimental de registro de time por cópia.
- Importação e exportação CSV de nomes de jogadores e times.
- Lista de competições existentes e avisos sobre as dependências ainda não mapeadas para criar uma liga jogável.
- O editor sempre salva as alterações de banco em um CPK novo; a instalação do jogo continua sendo uma ação separada.

## 0.1.0 — 2026-11-14

- Primeira versão pública do PES 2017 Editor.
- Interface Windows em Tkinter para selecionar a pasta do jogo e navegar pelos CPKs.
- Filtro por caminho interno, extração de arquivos individuais e extração completa.
- Reconstrução de CPK com conteúdo editado.
- Instalação opcional do CPK reconstruído com cópia de segurança do original.
- Leitura de tabelas CPK/UTF e descompressão de entradas CRILAYLA.
- Nenhum executável, CPK ou outro dado proprietário do jogo é distribuído pelo projeto.
