# PES 2017 EDITOR — Diagnóstico de inicialização

## Logs

Os logs ficam em:

```text
%LOCALAPPDATA%\PES2017EDITOR\logs
```

- `startup.log`: versão, arquitetura, OS, diretório, argumentos e criação da janela;
- `application.log`: eventos normais da aplicação;
- `crash.log`: exceções não tratadas e falhas de inicialização;
- `update.log`: operações do atualizador.

Os arquivos são rotacionados ao ultrapassar 1 MiB. O logger não impede a inicialização se o diretório estiver indisponível; nesse caso tenta `%TEMP%\PES2017EDITOR\logs`.

## Modo seguro

Execute:

```powershell
PES2017DatabaseEditor.exe --safe-mode
```

O modo seguro abre a interface sem carregar configurações persistentes, cache, instalações recentes ou verificação automática de atualizações. Nenhum dado é apagado.

## Validação automatizada

```powershell
pwsh .\scripts\verify-startup.ps1 -ExecutablePath "C:\Program Files\PES 2017 Database Editor\PES2017DatabaseEditor.exe" -WorkingDirectory "C:\Program Files\PES 2017 Database Editor"
```

O script exige processo ativo, handle de janela válido, janela visível, não minimizada, dimensões positivas e estado responsivo.
