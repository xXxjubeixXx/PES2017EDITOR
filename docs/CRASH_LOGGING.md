# PES 2017 EDITOR — Registro de falhas

O aplicativo registra falhas em `crash.log` e também usa os mecanismos nativos do .NET/WPF:

- `DispatcherUnhandledException` para exceções da interface;
- `AppDomain.UnhandledException` para falhas não tratadas do processo;
- `TaskScheduler.UnobservedTaskException` para tarefas assíncronas sem observador.

Falhas antes da criação da `MainWindow` geram mensagem informando o caminho do diagnóstico e encerram o processo com segurança. Falhas de serviços após a janela abrir são registradas e comunicadas sem ocultar a exceção.

Os logs não registram tokens, credenciais ou conteúdo de CPK/BIN. Caminhos locais podem aparecer porque são necessários para reproduzir o problema.
