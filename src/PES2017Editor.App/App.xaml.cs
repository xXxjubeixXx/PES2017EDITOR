using System.Windows.Threading;
using PES2017Editor.Infrastructure;

namespace PES2017Editor.App;

public partial class App : System.Windows.Application
{
    public static bool IsSafeMode { get; private set; }
    public static EditorLogger Logger { get; } = EditorLogger.Default;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        IsSafeMode = e.Args.Any(argument => argument.Equals("--safe-mode", StringComparison.OrdinalIgnoreCase));
        ShutdownMode = System.Windows.ShutdownMode.OnMainWindowClose;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        Logger.WriteStartupContext(IsSafeMode, e.Args);

        try
        {
            base.OnStartup(e);
            Logger.WriteStartup("Recursos WPF carregados; criando MainWindow.");
            var window = new MainWindow(IsSafeMode);
            MainWindow = window;
            window.Show();
            Logger.WriteStartup($"MainWindow criada e exibida. handle={new System.Windows.Interop.WindowInteropHelper(window).EnsureHandle()}.");
        }
        catch (Exception exception)
        {
            HandleFatalStartup("A criação da janela principal falhou.", exception);
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Logger.WriteCrash("Exceção não tratada no Dispatcher WPF.", e.Exception);
        if (MainWindow is null || !MainWindow.IsLoaded)
        {
            e.Handled = true;
            HandleFatalStartup("O aplicativo não conseguiu criar a interface principal.", e.Exception);
        }
        else
        {
            e.Handled = true;
            ShowError("Ocorreu um erro na interface. O diagnóstico foi salvo nos logs.");
        }
    }

    private void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e) =>
        Logger.WriteCrash("AppDomain.UnhandledException; o processo será encerrado.", e.ExceptionObject as Exception);

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Logger.WriteCrash("TaskScheduler.UnobservedTaskException.", e.Exception);
        e.SetObserved();
    }

    private void HandleFatalStartup(string message, Exception exception)
    {
        Logger.WriteCrash(message, exception);
        ShowError($"Não foi possível iniciar o PES 2017 Editor.\n\nConsulte o diagnóstico em:\n{Logger.CrashLogPath}");
        Shutdown(1);
    }

    private static void ShowError(string message)
    {
        try { System.Windows.MessageBox.Show(message, "PES 2017 Editor", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error); }
        catch { }
    }
}
