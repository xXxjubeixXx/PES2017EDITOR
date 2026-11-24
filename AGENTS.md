# Orientação para futuras IAs

## Regras permanentes de identidade e escopo

- Todos os prompts, relatórios e títulos de fases devem começar com `PES 2017 EDITOR`.
- Este projeto é independente de `PES 2017 SERVER` e `PES 2017 RADMIN`.
- Os estudos do projeto de expansão de ligas podem ser consultados, mas nunca modificados por este projeto.
- Nenhuma funcionalidade pode ser declarada concluída sem evidência reproduzível.
- Uma release não encerra uma fase por si só; cada versão deve apresentar progresso funcional verificável.
- Os arquivos originais do jogo devem permanecer preservados.
- O usuário não deve precisar extrair, converter, reconstruir ou manipular CPKs/BINs manualmente.
- Iniciar um processo não é evidência de inicialização aprovada: toda versão deve validar handle, visibilidade, dimensões e responsividade da MainWindow.
- Toda falha de inicialização deve deixar evidência em logs e ser reproduzível pelo script `scripts/verify-startup.ps1` quando o ambiente permitir.

## Escopo e segurança

- Trabalhe somente dentro de `PES2017EDITOR`, salvo leitura explícita de evidências no projeto pai.
- Não modifique, exclua ou sobrescreva o projeto de expansão de ligas nem os arquivos originais do jogo.
- A Fase 01 é somente leitura. Qualquer escrita futura deve usar cópia experimental, hash SHA-256, backup não sobrescrito, staging temporário e restauração transacional.
- Não copie ou redistribua `CpkMaker.DLL` sem licença verificável. O arquivo existente é usado apenas como referência local para investigação.
- Não atribua contagens, semântica de campos ou precedência de arquivos sem evidência reproduzível.

## Método

Classifique toda conclusão como **comprovada**, **parcialmente comprovada**, **hipótese**, **desconhecida** ou **contraditória**. Preserve bytes e campos desconhecidos. Para alterar um parser, crie ou atualize teste de leitura e round-trip quando o formato permitir.

## Arquitetura

`PES2017Editor.Core` contém contratos e fatos; `Cpk` acessa contêineres; `Binary` interpreta BINs; `Game` detecta instalações; `Database` mantém cache auxiliar; `Infrastructure` trata hash e instalações recentes; `App` contém apenas MVVM/WPF. O contrato `ICpkArchiveBackend` permite trocar o backend gerenciado por um helper x86 isolado sem acoplar a UI.

## Comandos

Use o SDK x64 explicitamente:

```powershell
& 'C:\Program Files\dotnet\dotnet.exe' build .\PES2017Editor.sln -c Release --no-restore
& 'C:\Program Files\dotnet\dotnet.exe' test .\PES2017Editor.sln -c Release --no-restore
```

Atualize `PROJECT_STATUS.md`, `TEST_RESULTS.md`, `CHANGELOG.md` e o relatório da fase quando houver uma mudança verificável.

## Processo obrigatório de distribuição

Para cada nova versão: implementar → testar → corrigir → documentar → atualizar changelog → incrementar `VersionPrefix` → compilar → gerar instalador → gerar ZIP portátil → calcular hashes → atualizar manifesto → inspecionar staging → commit → push → tag → release → verificar arquivos remotos → confirmar que o atualizador identifica a release. Nunca publique CPKs, BINs, saves, backups, tokens, certificados privados ou DLLs sem licença.

O repositório oficial é `https://github.com/xXxjubeixXx/PES2017EDITOR`. Preserve o histórico e crie uma referência de backup antes de substituir uma árvore antiga. Não faça force push sem autorização explícita.

