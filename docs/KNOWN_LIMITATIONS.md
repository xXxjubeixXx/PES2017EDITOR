# PES 2017 EDITOR — Limitações conhecidas

## CPK

- As TOCs dos 55 CPKs da instalação de referência são legíveis, mas isso não comprova a interpretação de todo payload interno.
- A escrita é uma cópia layout-preserving; não cria entradas nem reconstrói TOC/ITOC/ETOC/GTOC.
- CRILAYLA e outros codecs não estão integrados ao backend principal.
- A extração individual entrega bytes do slot conforme o CPK; a descompressão depende do codec/contêiner e não é prometida universalmente.

## Banco

- Player, Team, Competition e PlayerAssignment possuem somente os campos comprovados documentados em `BINARY_FORMATS.md`.
- Apenas o nome do jogador é editável.
- Atributos, aparências, ligas, promoção/rebaixamento, escalações e táticas continuam sem escrita.
- Os relacionamentos de jogador–time são parciais; time–liga e competição–participante ainda não estão comprovados.
- Os números observados dependem da instalação, Data Pack e patch.

## Jogo

- A precedência efetiva entre Data, download, Data Packs, patches, EDIT e saves ainda não foi comprovada de forma completa.
- Não há confirmação visual dentro do jogo da edição v0.3.1.
- Não há modificação do `PES2017.exe`.

## Distribuição e segurança

- `CpkMaker.DLL` não é distribuída por licença/procedência não verificadas.
- Authenticode e assinatura de manifesto ainda não estão disponíveis; SHA-256 comprova integridade do arquivo recebido, não a identidade do publicador.
- Os testes proprietários usam arquivos locais e não podem ser executados no CI público.
- A instalação sobre v0.3.0 e a inicialização pelos atalhos foram testadas; atualização automática baixada e recuperação após falha ainda não foram executadas.
- A validação visual da interface foi feita por Win32 `PrintWindow` e métricas da janela; uma sessão de automação WPF por UI Automation ainda não foi disponibilizada neste ambiente.

## Classificação

Informações não listadas como comprovadas devem ser consideradas parcialmente comprovadas, hipótese ou desconhecidas até que exista uma evidência reproduzível. Nenhum limite de quantidade foi introduzido artificialmente pelo editor.
