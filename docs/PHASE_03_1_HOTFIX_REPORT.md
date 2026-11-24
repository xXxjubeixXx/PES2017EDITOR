# PES 2017 EDITOR — Relatório do hotfix Fase 03.1

Data: 24/11/2026. Versão: `0.3.1`.

## Causa comprovada

A v0.3.0 foi reproduzida pelo atalho público, pelo executável instalado e pelo portátil oficial do GitHub. O processo encerrava antes de exibir a janela com código `0xe0434352` / `-532462766`.

O Event Viewer registrou `.NET Runtime 1026` e `Application Error 1000`. A exceção foi:

```text
System.InvalidOperationException:
Uma associação TwoWay ou OneWayToSource não pode funcionar na propriedade somente leitura 'Progress' do tipo MainViewModel.
```

Após a primeira correção, a publicação local revelou a mesma falha em `UpdateProgress`. Ambas as associações foram corrigidas explicitamente para `Mode=OneWay`.

## Correções

- removido `StartupUri` automático;
- criação controlada da `MainWindow` em `App.OnStartup`;
- captura de `DispatcherUnhandledException`, `AppDomain.UnhandledException` e `TaskScheduler.UnobservedTaskException`;
- logs `startup.log`, `application.log`, `crash.log` e `update.log` em `%LOCALAPPDATA%\PES2017EDITOR\logs`;
- rotação de logs acima de 1 MiB;
- mensagem compreensível para falhas fatais;
- modo `--safe-mode`, ignorando configurações, cache e verificação automática;
- painel de diagnóstico com caminho do log, abrir pasta e copiar caminho;
- teste permanente para os bindings unidirecionais;
- script `scripts/verify-startup.ps1` com validação de handle, visibilidade, tamanho e responsividade.

## Evidência antes/depois

| Artefato | v0.3.0 | v0.3.1 |
|---|---|---|
| Instalado | falha antes da janela | janela visível e responsiva |
| Atalho Desktop | falha | aprovado |
| Atalho Menu Iniciar | falha | aprovado |
| Portátil GitHub | encerra com `-532462766` | janela visível e responsiva |
| Build local publicado | não validado visualmente | handle válido, 1440×900 |
| `--safe-mode` | inexistente | handle válido, 1440×900 |

Validação da instalação atualizada:

- registro Windows: versão `0.3.1`;
- executável: `C:\Program Files\PES 2017 Database Editor\PES2017DatabaseEditor.exe`;
- título: `PES 2017 Database Editor`;
- janela: visível, não minimizada, responsiva, 1440×900;
- atalho Desktop público: aprovado;
- atalho Menu Iniciar comum: aprovado;
- screenshot real: `artifacts/hotfix-interface-v0.3.1-printwindow.png`.

## Testes

- Build Release: 0 erros, 0 avisos.
- Testes xUnit: 18 aprovados, 0 reprovados, 0 ignorados.
- Executável de desenvolvimento: aprovado.
- Publish self-contained: aprovado.
- Executável instalado após upgrade: aprovado.
- Atalho Desktop e Menu Iniciar: aprovados.
- Modo seguro: aprovado.
- Instalação sobre v0.3.0: código 0, versão registrada `0.3.1`.
- Portátil publicado pelo GitHub Actions: hash validado, janela visível e responsiva em 1440×900.

## Publicação

- Commit da correção: `edfacd8cfe3fc588cd23227ec093b2e09208bd91`.
- Tag: `v0.3.1`.
- Canal: pré-release experimental.
- CI: [45510140659](https://github.com/xXxjubeixXx/PES2017EDITOR/actions/runs/45510140659), aprovado.
- Release: [PES 2017 Database Editor v0.3.1](https://github.com/xXxjubeixXx/PES2017EDITOR/releases/tag/v0.3.1).
- Instalador publicado: 68.269.607 bytes, SHA-256 `5a7541aeb2240e6a8776ebaa36bb7c57d9cee32376c386dbdb27079adfbaabc0`.
- Portátil publicado: 95.245.538 bytes, SHA-256 `302ae0f46daab2433b814a8eb1d2757fa27659938642d34f0aaca922761018e7`.
- `SHA256SUMS.txt`: 231 bytes; `update-manifest.json`: 973 bytes.
- Código-fonte automático: [ZIP](https://github.com/xXxjubeixXx/PES2017EDITOR/archive/refs/tags/v0.3.1.zip) e [TAR.GZ](https://github.com/xXxjubeixXx/PES2017EDITOR/archive/refs/tags/v0.3.1.tar.gz).

## Pendências

O teste in-game continua pendente. A v0.3.1 deve permanecer pré-release experimental. A assinatura Authenticode ainda não está configurada.
