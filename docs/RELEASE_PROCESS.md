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
git commit -m "release: v0.2.0"
git push origin main
git tag -a v0.2.0 -m "PES 2017 Database Editor v0.2.0"
git push origin v0.2.0
pwsh .\scripts\release.ps1 -PrepareOnly
gh release create v0.2.0 `
  artifacts/installer/PES2017DatabaseEditor-Setup-v0.2.0-win-x64.exe `
  artifacts/release/0.2.0/PES2017DatabaseEditor-Portable-v0.2.0-win-x64.zip `
  artifacts/release/0.2.0/SHA256SUMS.txt `
  artifacts/release/0.2.0/update-manifest.json `
  --repo xXxjubeixXx/PES2017EDITOR `
  --title "PES 2017 Database Editor v0.2.0" `
  --notes-file docs/PHASE_02_REPORT.md
```

O workflow `release.yml` automatiza a etapa após uma tag compatível. Antes do primeiro push, inspecione `git status`, `git diff --cached` e confirme que não há CPK, BIN, save, token, certificado ou build local preparado.

## Verificação remota

```powershell
gh api repos/xXxjubeixXx/PES2017EDITOR/commits/main --jq .sha
gh release view v0.2.0 --repo xXxjubeixXx/PES2017EDITOR
gh release view v0.2.0 --repo xXxjubeixXx/PES2017EDITOR --json assets,tagName,url
```

