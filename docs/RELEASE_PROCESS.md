# Processo de release

## Verificações locais

```powershell
gh auth status
pwsh .\scripts\build.ps1
pwsh .\scripts\test.ps1
pwsh .\scripts\release.ps1 -PrepareOnly
```

O preparo gera instalador, ZIP portátil, `SHA256SUMS.txt` e `update-manifest.json` em `artifacts/release/<versão>`.

## Publicação manual autorizada

```powershell
git status --short
git add -A
git commit -m "release: v<versao>"
git push origin main
git tag -a v<versao> -m "PES 2017 Database Editor v<versao>"
git push origin v<versao>
pwsh .\scripts\release.ps1 -PrepareOnly
gh release create v<versao> `
  artifacts/installer/PES2017DatabaseEditor-Setup-v<versao>-win-x64.exe `
  artifacts/release/<versao>/PES2017DatabaseEditor-Portable-v<versao>-win-x64.zip `
  artifacts/release/<versao>/SHA256SUMS.txt `
  artifacts/release/<versao>/update-manifest.json `
  --repo xXxjubeixXx/PES2017EDITOR `
  --title "PES 2017 Database Editor v<versao>" `
  --notes-file docs/PHASE_02_REPORT.md
```

O workflow `release.yml` automatiza a etapa após uma tag compatível. Antes do primeiro push, inspecione `git status`, `git diff --cached` e confirme que não há CPK, BIN, save, token, certificado ou build local preparado.

## Verificação remota

```powershell
gh api repos/xXxjubeixXx/PES2017EDITOR/commits/main --jq .sha
gh release view v<versao> --repo xXxjubeixXx/PES2017EDITOR
gh release view v<versao> --repo xXxjubeixXx/PES2017EDITOR --json assets,tagName,url
pwsh .\scripts\verify-release.ps1
```

