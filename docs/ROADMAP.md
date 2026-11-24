# Roadmap

## Fase 02 — distribuição local concluída; publicação em andamento

- instalador e versão portátil self-contained;
- versionamento centralizado;
- atualizador GitHub com manifesto e SHA-256;
- CI/CD e scripts reproduzíveis;
- documentação e testes do instalador.

## Fase 01 — concluída com escopo parcial explícito

- solução .NET 10/WPF/MVVM modular;
- detecção da instalação e instalações recentes;
- indexação virtual CPK, CRI UTF e entradas nativas;
- exploração, filtro e extração individual;
- preview hexadecimal e envelope WESYS;
- cache SQLite e testes reais;
- documentação e publicação inicial.

## Fase 02 — prioridade

1. Integrar um backend x86 isolado somente leitura, condicionado a licença e detecção local.
2. Implementar descompressão comprovada de codecs usados pela instalação.
3. Criar leitores de BIN com preservação de bytes desconhecidos e testes round-trip.
4. Identificar os bancos de jogadores/times/competições por evidência, não por nome.
5. Implementar visualizadores de texto/imagem seguros.
6. Instrumentar precedência e arquivo efetivamente carregado.

## Fases posteriores

Validação relacional, edição estruturada, geração de patch separado, reconstrução CPK, backups e aplicação controlada. Só depois de testes em cópias experimentais considerar integração com a instalação real.

