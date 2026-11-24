# Distribuição

Cada release estável deve conter:

- `PES2017DatabaseEditor-Setup-v<versão>-win-x64.exe`;
- `PES2017DatabaseEditor-Portable-v<versão>-win-x64.zip`;
- `SHA256SUMS.txt`;
- `update-manifest.json`;
- código-fonte automático do GitHub em ZIP e TAR.GZ.

O instalador é o canal recomendado. A versão portátil contém o executável principal, o helper de atualização e o README. Ambos são self-contained x64.

Não são distribuídos `PES2017.exe`, CPKs, BINs, saves, backups, `CpkMaker.DLL` ou outras ferramentas proprietárias. Artefatos locais são ignorados pelo Git e só entram em releases depois de hashes e inspeção.

