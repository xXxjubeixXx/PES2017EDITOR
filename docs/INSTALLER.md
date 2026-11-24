# Instalador Windows

O instalador é gerado com Inno Setup 6.7.3. A escolha atende instalação por máquina, registro em Aplicativos instalados, atalhos, atualização sobre a mesma instalação e desinstalação sem acoplar dados do usuário a `Program Files`.

## Características

- Windows 10/11 x64;
- interface brasileira do Inno Setup;
- `Program Files\PES 2017 Database Editor` como diretório padrão;
- AppId permanente para reconhecer versões anteriores;
- atalho no Menu Iniciar;
- atalho opcional na área de trabalho;
- opção de iniciar o aplicativo ao terminar;
- desinstalador registrado pelo Windows;
- atualização preservando `%LOCALAPPDATA%\PES2017EDITOR`;
- aplicação self-contained, incluindo o helper `PES2017Editor.Updater.exe`.

O código-fonte está em `installer/PES2017DatabaseEditor.iss`. A compilação é automatizada por `scripts/build-installer.ps1`, que lê `VersionPrefix` do `Directory.Build.props` e não mantém uma versão paralela no script.

## Teste local

```powershell
pwsh .\scripts\publish.ps1 -OutputDirectory artifacts/installer-payload
pwsh .\scripts\build-installer.ps1 -SkipPublish
```

O instalador é produzido em `artifacts/installer/` e é ignorado pelo Git. Use uma pasta de teste isolada para validar instalação, atalhos, inicialização e desinstalação.

