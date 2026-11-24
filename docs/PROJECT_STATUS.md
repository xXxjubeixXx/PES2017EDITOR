# Estado do projeto

Última atualização: 24/11/2026.

## Estado atual

Fase 01 implementada e Fase 02 implementada para distribuição. A solução compila em Release com o SDK x64 do .NET 10. A versão centralizada é `0.2.1`, correção de empacotamento posterior à release `v0.2.0`. O instalador Inno Setup e o sistema de atualização foram implementados e testados em ambiente isolado.

## Implementado

- solução modular e WPF/MVVM;
- detector da instalação;
- recentes;
- CPK/CRI UTF/TOC nativos;
- indexação assíncrona e cache SQLite;
- explorador, filtro, duplicatas e extração individual;
- preview hexadecimal e parser WESYS inicial;
- dashboard com fatos reais;
- navegação preparada para as áreas futuras;
- testes reais contra a instalação original.

## Verificado

- 55 CPKs encontrados;
- 159 entradas indexadas pelo backend atual;
- `dt10_win.cpk`: 156 entradas nativas;
- `dt43_jpn_win.cpk`: 3 entradas nativas;
- 9 testes aprovados localmente, incluindo 4 testes públicos e 5 testes com fixtures proprietárias;
- extração sem alteração do CPK de origem;
- cache SQLite criado em diretório temporário de teste.

## Fase 02 verificada

- repositório remoto auditado;
- branch de arquivo `archive/pre-phase-02-20261124` criada antes da substituição;
- instalador Inno Setup compilado;
- instalação, atalhos, execução e desinstalação isoladas aprovadas;
- serviço de atualização e manifesto implementados;
- workflows `ci.yml` e `release.yml` criados;
- tag/release `v0.2.0` preservada e confirmada remotamente;
- tag/release `v0.2.1` publicada e confirmada remotamente;
- commit da implementação publicado: `1df85a4f13a008da9f363c345f4c630809508a8c`;
- release principal: https://github.com/xXxjubeixXx/PES2017EDITOR/releases/tag/v0.2.1;
- branch de backup: `archive/pre-phase-02-20261124` → `7eef9445fd4177963a1c996b0cf0a1997e8dcff2`.

Executável publicado: `artifacts/publish/win-x64/PES2017DatabaseEditor.exe` (self-contained, win-x64, SHA-256 `BD40784B0948057610D451025BE0A6A67A05882E1E291608A0B8744F263B36D9`).

## Problemas existentes

53 CPKs têm TOC mascarada para o backend gerenciado. Não há parser de entidades de banco, descompressão completa, edição, reconstrução ou aplicação de patch. A dependência SQLite emite NU1903 transitivo. Precedência de arquivos permanece desconhecida.

## Decisões

- .NET 10/WPF foi mantido por compatibilidade com Windows 10/11 e separação clara entre núcleo e UI.
- O aplicativo principal é x64.
- O backend CPK é substituível por `ICpkArchiveBackend`.
- O SQLite é cache, nunca fonte final do jogo.
- Nenhuma DLL externa foi redistribuída.

## Comandos

```powershell
& 'C:\Program Files\dotnet\dotnet.exe' restore .\PES2017Editor.sln
& 'C:\Program Files\dotnet\dotnet.exe' build .\PES2017Editor.sln -c Release --no-restore
& 'C:\Program Files\dotnet\dotnet.exe' test .\PES2017Editor.sln -c Release --no-restore
```

## Próximas etapas

Baixar e testar a atualização real de uma versão anterior em ambiente isolado e depois retomar TOCs mascaradas, descompressão, parsers BIN, precedência e pipeline transacional de escrita.

