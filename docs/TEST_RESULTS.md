# Resultados de testes

Última atualização: 24/11/2026.

Última execução registrada após a implementação da Fase 02:

```text
dotnet test PES2017Editor.sln -c Release --logger "console;verbosity=normal"
Resultado: aprovado
Testes: 9 passed, 0 failed
```

No GitHub Actions, os testes públicos foram executados separadamente com `--filter "Category!=Proprietary"`: 4 passed, 0 failed. Os 5 testes `Proprietary` exigem a instalação original local e passaram na execução local acima.

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

O pacote self-contained Windows x64 foi publicado em `artifacts/installer-payload/PES2017DatabaseEditor.exe`, com 142.821.072 bytes e versão 0.2.1. Na release remota `v0.2.1`, o instalador tem 68.188.344 bytes e SHA-256 `6CB3F506ECA85265D71C079593CC374B1A1B7B0AA8B04C914D13C82521ED42EB`; o ZIP portátil tem 95.092.388 bytes e SHA-256 `0DD0D36E78F10A2413B1E105840C8187B0190CB356850FBE01C4A1B9AC2B4B8C`.

Um smoke test iniciou o processo por aproximadamente três segundos e confirmou que ele permaneceu ativo; o processo de teste foi encerrado sem tocar no jogo.

Ainda não existem testes de reconstrução CPK, descompressão completa, round-trip BIN, restauração de backup, cancelamento de operação longa ou aplicação de patch, pois essas capacidades não foram implementadas.

Os testes isolados do instalador estão em `docs/INSTALLER_TEST_RESULTS.md` e cobrem instalação, atalhos, inicialização e desinstalação.

