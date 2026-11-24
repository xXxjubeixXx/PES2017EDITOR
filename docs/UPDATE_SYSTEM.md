# Sistema de atualização

## Fonte oficial

O serviço consulta somente:

`https://api.github.com/repos/xXxjubeixXx/PES2017EDITOR/releases/latest`

O repositório é público. Não há token no executável. A requisição usa HTTPS, User-Agent identificável, timeout de 15 segundos e não bloqueia a abertura da janela.

## Fluxo

1. A versão instalada vem do `AssemblyInformationalVersion` gerado pelo `Directory.Build.props`.
2. A resposta da release é filtrada para rejeitar draft/prerelease.
3. Tags são interpretadas como SemVer, aceitando o prefixo `v`.
4. O serviço procura o instalador `*-win-x64.exe` e, quando presente, lê também `update-manifest.json`.
5. A interface mostra versão, notas e tamanho.
6. O usuário escolhe **Atualizar agora**.
7. O download é salvo em `%LOCALAPPDATA%\PES2017EDITOR\updates`.
8. Tamanho e SHA-256 são verificados antes de qualquer execução.
9. `PES2017Editor.Updater.exe` aguarda o processo principal terminar.
10. O helper inicia o instalador e o Inno Setup atualiza a pasta do programa.

Falhas de rede, GitHub indisponível, cancelamento e hash inválido deixam o aplicativo instalado e registram uma entrada em `update-history.json`. Atualização automática pode ser desativada; a ação de download/execução nunca é automática.

## Segurança

O serviço aceita apenas URLs HTTPS de `github.com`, subdomínios GitHub e `githubusercontent.com`. Hash é verificação de integridade, não assinatura do publicador. Authenticode e assinatura do manifesto ainda não estão disponíveis e não são simulados.

