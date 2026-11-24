# Versionamento

O projeto usa SemVer e mantém uma única fonte de verdade:

`Directory.Build.props` → `VersionPrefix = 0.2.1`

O SDK .NET deriva automaticamente `Version`, `AssemblyVersion`, `FileVersion` e `InformationalVersion`. `AppMetadata` lê os metadados do assembly para a UI e para o User-Agent. `scripts/build-installer.ps1` e `scripts/release.ps1` leem o mesmo `VersionPrefix` para nomes, instalador, manifesto e tag.

## Regras

- mudança incompatível: MAJOR;
- funcionalidade compatível: MINOR;
- correção compatível: PATCH;
- prerelease usa sufixo SemVer, mas o atualizador ignora prereleases;
- tag regular: `vMAJOR.MINOR.PATCH`;
- release e artefatos não devem reutilizar silenciosamente uma tag já publicada.

A versão do workflow de release precisa corresponder à tag; o script falha se `GITHUB_REF_NAME` divergir de `VersionPrefix`.

