# Limitações conhecidas

- A Fase 01 é somente leitura; não há edição de BIN, patch, backup transacional ou reconstrução CPK.
- Apenas TOCs `@UTF` nativas são decodificadas pelo backend gerenciado. CPKs com TOC mascarada são indexadas como parciais.
- Não há descompressão completa de todos os codecs CPK/CRILAYLA/WESYS.
- Extração atual copia bytes armazenados; não garante saída descomprimída quando `Codec` indica compressão.
- Visualização de texto, imagens e edição estruturada ainda não está disponível.
- Não existem contagens comprovadas de jogadores, times, ligas, competições, estádios ou treinadores.
- Precedência real de arquivos e arquivo efetivamente carregado não foram comprovados.
- A DLL x86 encontrada é local, antiga e com redistribuição não esclarecida; não foi incorporada.
- O pacote SQLite atual emite NU1903 transitivo para `SQLitePCLRaw.lib.e_sqlite3 2.1.11`; a vulnerabilidade precisa ser resolvida ou o cache substituído/atualizado antes de uma distribuição final.
- O executável gerado requer Windows x64; a validação visual automatizada em WPF deve ser ampliada na próxima fase.
- Authenticode e assinatura criptográfica do manifesto ainda não estão configurados; SHA-256 não autentica o publicador.
- A atualização real contra uma release pública ainda precisa ser testada após a primeira publicação da `v0.2.0`.

