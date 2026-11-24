# Resultados de testes

Última atualização: 24/11/2026.

Última execução registrada após a implementação da Fase 01:

```text
dotnet test PES2017Editor.sln -c Release --logger "console;verbosity=normal"
Resultado: aprovado
Testes: 9 passed, 0 failed
```

Cobertura funcional dos testes:

- detecção da instalação original;
- leitura de cabeçalho e TOC nativa de `dt10_win.cpk`;
- indexação dos 55 CPKs reais sem extração integral;
- total observado de 159 entradas nativas/parcialmente indexáveis;
- extração de `Competition.bin` sem alteração binária no CPK;
- fallback raw e reconhecimento WESYS;
- persistência do índice no SQLite temporário.
- ordenação SemVer e rejeição lógica de prerelease;
- persistência das preferências em `%LOCALAPPDATA%\PES2017EDITOR`.

Também foram executadas compilações Release da solução. O build foi aprovado com aviso NU1903 transitivo do SQLite, documentado em `KNOWN_LIMITATIONS.md`; o aviso não foi ocultado como se fosse uma validação de segurança.

O pacote self-contained Windows x64 foi publicado em `artifacts/installer-payload/PES2017DatabaseEditor.exe`, com 142.821.072 bytes. O instalador Inno Setup tem 68.180.065 bytes e SHA-256 `1BC8158240C551567AA4FB7588D09EF386E03410E26346AB8FFFBE180B01B6CA`. O ZIP portátil tem 95.091.817 bytes e SHA-256 `7B9BCDD11DFD6AA6FCA669B1C67C010E5AE96C27294D849AE439758F757C472E`.

Um smoke test iniciou o processo por aproximadamente três segundos e confirmou que ele permaneceu ativo; o processo de teste foi encerrado sem tocar no jogo.

Ainda não existem testes de reconstrução CPK, descompressão completa, round-trip BIN, restauração de backup, cancelamento de operação longa ou aplicação de patch, pois essas capacidades não foram implementadas.

Os testes isolados do instalador estão em `docs/INSTALLER_TEST_RESULTS.md` e cobrem instalação, atalhos, inicialização e desinstalação.

