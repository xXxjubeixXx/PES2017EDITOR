# PES 2017 EDITOR — Roadmap

## Fase 03 — motor de dados e primeira edição experimental

Implementado e verificado parcialmente:

- TOC CRI UTF mascarada;
- indexação dos 55 CPKs e 40.505 entradas da instalação de referência;
- WESYS/zlib e payload direto;
- leitura real de jogadores, times, competições e associações;
- edição segura do nome do jogador em CPK experimental;
- reabertura, hashes, backup e preservação dos originais;
- correção da auditoria de dependências SQLite.

Pendente para encerrar integralmente a fase: teste in-game, precedência comprovada e reconstrução geral de CPK.

## Próxima etapa

1. Relatório de compatibilidade por CPK com entradas verificadas e codecs.
2. Inspetor de registros com offset, bytes e campos conhecidos/desconhecidos.
3. Diagnóstico de IDs duplicados, órfãos e referências inválidas.
4. Parser diferencial de atributos e relacionamentos adicionais.
5. Reconstrução transacional de CPK em pasta de patch separada.
6. Teste isolado da instalação e do atualizador da v0.3.0.

## Expansão futura

- editar times e competições somente quando os campos forem comprovados;
- separar limites do BIN, IDs, tabelas e executável;
- suportar criação de registros sem substituir existentes quando o formato e o executável permitirem;
- comparar bancos original/experimental sem alterar o projeto paralelo;
- investigar promoção, rebaixamento, descritores visuais e Master League.

## Regra de qualidade

Cada marco deve possuir evidência reproduzível, teste automatizado ou manual registrado e classificação explícita de limitações. Uma release, sozinha, não encerra uma fase.
