using System.Globalization;
using System.Windows;
using System.Windows.Data;
using PES2017Editor.App.ViewModels;
using PES2017Editor.Infrastructure;

namespace PES2017Editor.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(bool safeMode = false)
    {
        _viewModel = new MainViewModel(safeMode);
        InitializeComponent();
        DataContext = _viewModel;
        Loaded += OnLoaded;
        Closed += (_, _) => EditorLogger.Default.WriteApplication("MainWindow fechada; encerramento normal.");
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            EditorLogger.Default.WriteStartup("MainWindow carregada; inicializando serviços secundários.");
            await _viewModel.InitializeAsync();
            EditorLogger.Default.WriteStartup("Inicialização concluída; interface responsiva.");
        }
        catch (Exception exception)
        {
            EditorLogger.Default.WriteCrash("Falha durante a inicialização assíncrona da MainWindow.", exception);
            System.Windows.MessageBox.Show(
                $"O aplicativo abriu, mas não conseguiu inicializar todos os serviços.\n\nLog: {EditorLogger.Default.CrashLogPath}",
                "PES 2017 Editor",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}

public sealed class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class StringEqualsVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class UnavailablePageVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var current = value?.ToString();
        var supportedPages = (parameter?.ToString() ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return supportedPages.Contains(current, StringComparer.OrdinalIgnoreCase)
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
