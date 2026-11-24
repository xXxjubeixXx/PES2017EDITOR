using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Win32;
using PES2017Editor.Binary;
using PES2017Editor.Core;
using PES2017Editor.Cpk;
using PES2017Editor.Database;
using PES2017Editor.Game;
using PES2017Editor.Infrastructure;

namespace PES2017Editor.App.ViewModels;

public sealed class MainViewModel : NotifyPropertyChanged
{
    private readonly GameInstallationDetector _detector = new();
    private readonly CpkIndexService _indexer = new();
    private readonly CpkArchiveReader _reader = new();
    private readonly RecentInstallationsStore _recentStore = new();
    private readonly BinaryParserRegistry _binaryParsers = new();
    private readonly EditorSettingsStore _settingsStore = new();
    private readonly GitHubUpdateService _updateService = new();
    private readonly UpdateHistoryStore _updateHistory = new();
    private CancellationTokenSource? _loadCancellation;
    private EditorSettings _settings = new();
    private UpdateInfo? _availableUpdate;
    private string _selectedPath = string.Empty;
    private string _selectedPage = "Visão geral";
    private string _filterText = string.Empty;
    private string _status = "Selecione uma instalação do PES 2017 para começar.";
    private bool _isLoading;
    private bool _isCheckingUpdates;
    private bool _isUpdating;
    private bool _autoCheckForUpdates = true;
    private double _progress;
    private double _updateProgress;
    private string _updateStatus = "Atualizações ainda não verificadas.";
    private InstallationInfo? _installation;
    private DashboardFacts _facts = new(0, 0, null, 0, 0, null, SupportStatus.NotSupported, []);

    public MainViewModel()
    {
        BrowseCommand = new AsyncCommand(BrowseAsync);
        LoadCommand = new AsyncCommand(LoadAsync, () => !IsLoading && !string.IsNullOrWhiteSpace(SelectedPath));
        CancelCommand = new RelayCommand(_ => Cancel(), _ => IsLoading);
        ExtractCommand = new AsyncCommand<object?>(ExtractAsync);
        CheckUpdatesCommand = new AsyncCommand(() => CheckForUpdatesAsync(automatic: false), () => !IsCheckingUpdates && !IsUpdating);
        UpdateNowCommand = new AsyncCommand(DownloadAndLaunchUpdateAsync, () => IsUpdateAvailable && !IsUpdating && !IsCheckingUpdates);
        OpenRepositoryCommand = new RelayCommand(parameter => OpenUrl(parameter));
        Navigation = new ObservableCollection<NavigationItem>([
            new("Visão geral", "⌂"), new("Jogadores", "⚽"), new("Times", "▣"), new("Ligas", "◈"),
            new("Competições", "◇"), new("Seleções nacionais", "◎"), new("Estádios", "▤"),
            new("Escalações", "☷"), new("Transferências", "⇄"), new("Treinadores", "♟"),
            new("Árbitros", "◉"), new("Arquivos CPK", "▤"), new("Arquivos BIN", "▥"),
            new("Importação e exportação", "⇅"), new("Expansão do jogo", "✦"), new("Validação", "✓"),
            new("Histórico e backups", "◷"), new("Configurações", "⚙"), new("Sobre", "ⓘ")
        ]);
        SelectedNavigation = Navigation[0];
    }

    public ObservableCollection<NavigationItem> Navigation { get; }
    public ObservableCollection<string> RecentInstallations { get; } = [];
    public ObservableCollection<CpkArchiveRow> Archives { get; } = [];
    public ObservableCollection<CpkEntryRow> Entries { get; } = [];
    public ObservableCollection<IntegrityIssue> Issues { get; } = [];
    public ObservableCollection<BinaryField> PreviewFields { get; } = [];

    public ICommand BrowseCommand { get; }
    public ICommand LoadCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ExtractCommand { get; }
    public ICommand CheckUpdatesCommand { get; }
    public ICommand UpdateNowCommand { get; }
    public ICommand OpenRepositoryCommand { get; }

    public string SelectedPath
    {
        get => _selectedPath;
        set
        {
            if (Set(ref _selectedPath, value))
                (LoadCommand as AsyncCommand)?.RaiseCanExecuteChanged();
        }
    }
    public string SelectedPage
    {
        get => _selectedPage;
        set
        {
            if (Set(ref _selectedPage, value))
                OnPropertyChanged(nameof(SelectedNavigation));
        }
    }
    public string FilterText
    {
        get => _filterText;
        set { if (Set(ref _filterText, value)) RefreshEntries(); }
    }
    public string Status { get => _status; private set => Set(ref _status, value); }
    public bool IsLoading { get => _isLoading; private set { if (Set(ref _isLoading, value)) RaiseCommands(); } }
    public double Progress { get => _progress; private set => Set(ref _progress, value); }
    public double UpdateProgress { get => _updateProgress; private set => Set(ref _updateProgress, value); }
    public bool IsCheckingUpdates { get => _isCheckingUpdates; private set { if (Set(ref _isCheckingUpdates, value)) RaiseCommands(); } }
    public bool IsUpdating { get => _isUpdating; private set { if (Set(ref _isUpdating, value)) RaiseCommands(); } }
    public bool AutoCheckForUpdates
    {
        get => _autoCheckForUpdates;
        set
        {
            if (!Set(ref _autoCheckForUpdates, value)) return;
            _settings.AutoCheckForUpdates = value;
            _ = SaveSettingsAsync();
        }
    }
    public string BackupsDirectory
    {
        get => _settings.BackupsDirectory ?? AppDataPaths.BackupsDirectory;
        set { if (_settings.BackupsDirectory == value) return; _settings.BackupsDirectory = value; OnPropertyChanged(); _ = SaveSettingsAsync(); }
    }
    public string ProjectsDirectory
    {
        get => _settings.ProjectsDirectory ?? AppDataPaths.ProjectsDirectory;
        set { if (_settings.ProjectsDirectory == value) return; _settings.ProjectsDirectory = value; OnPropertyChanged(); _ = SaveSettingsAsync(); }
    }
    public string LargeFilesDirectory
    {
        get => _settings.LargeFilesDirectory ?? Path.Combine(AppDataPaths.Root, "large-files");
        set { if (_settings.LargeFilesDirectory == value) return; _settings.LargeFilesDirectory = value; OnPropertyChanged(); _ = SaveSettingsAsync(); }
    }
    public InstallationInfo? Installation { get => _installation; private set => Set(ref _installation, value); }
    public DashboardFacts Facts { get => _facts; private set => Set(ref _facts, value); }
    public NavigationItem? SelectedNavigation
    {
        get => Navigation.FirstOrDefault(x => x.Title == SelectedPage);
        set { if (value is not null) SelectedPage = value.Title; OnPropertyChanged(); }
    }
    public CpkEntryRow? SelectedEntry { get; set; }
    public string AppVersionText => $"v{AppMetadata.Version}";
    public string BuildInformationText => $"{AppMetadata.Architecture} · compilado em {AppMetadata.BuildDate}";
    public string UpdateStatus { get => _updateStatus; private set => Set(ref _updateStatus, value); }
    public bool IsUpdateAvailable => _availableUpdate is not null;
    public string LatestVersionText => _availableUpdate is null ? "Não identificada" : $"v{_availableUpdate.Version}";
    public string LatestReleaseNotes => _availableUpdate?.ReleaseNotes ?? "Nenhuma novidade carregada.";
    public string LatestInstallerSizeText => _availableUpdate is null || _availableUpdate.InstallerSize <= 0
        ? "Tamanho não informado"
        : FormatBytes(_availableUpdate.InstallerSize);
    public string IndexedCountText => Facts.IndexedFileCount == 0 ? "Não identificado" : Facts.IndexedFileCount.ToString("N0");
    public string CpkCountText => Facts.CpkCount == 0 ? "0" : Facts.CpkCount.ToString("N0");
    public string TotalSizeText => Facts.TotalCpkBytes == 0 ? "Não identificado" : FormatBytes(Facts.TotalCpkBytes);
    public string CompatibilityText => Facts.Compatibility switch
    {
        SupportStatus.Supported => "Compatível",
        SupportStatus.PartiallySupported => "Parcial — revisar avisos",
        SupportStatus.Invalid => "Inválida",
        _ => "Não identificada"
    };

    public async Task InitializeAsync()
    {
        _settings = await _settingsStore.LoadAsync();
        _autoCheckForUpdates = _settings.AutoCheckForUpdates;
        OnPropertyChanged(nameof(AutoCheckForUpdates));
        OnPropertyChanged(nameof(BackupsDirectory));
        OnPropertyChanged(nameof(ProjectsDirectory));
        OnPropertyChanged(nameof(LargeFilesDirectory));
        foreach (var path in await _recentStore.LoadAsync()) RecentInstallations.Add(path);
        if (_settings.AutoCheckForUpdates &&
            (!_settings.LastUpdateCheckUtc.HasValue || DateTimeOffset.UtcNow - _settings.LastUpdateCheckUtc.Value >= TimeSpan.FromHours(Math.Max(1, _settings.UpdateCheckIntervalHours))))
            _ = CheckForUpdatesAsync(automatic: true);
    }

    private Task BrowseAsync()
    {
        var dialog = new OpenFolderDialog { Title = "Selecione a pasta de instalação do PES 2017" };
        if (dialog.ShowDialog() == true) SelectedPath = dialog.FolderName;
        return Task.CompletedTask;
    }

    private async Task LoadAsync()
    {
        _loadCancellation?.Dispose();
        _loadCancellation = new CancellationTokenSource();
        var token = _loadCancellation.Token;
        IsLoading = true;
        Progress = 0;
        try
        {
            Status = "Verificando instalação...";
            Installation = await _detector.DetectAsync(SelectedPath, token);
            Progress = 8;
            Status = "Identificando CPKs...";
            var progress = new Progress<ProgressUpdate>(update =>
            {
                Status = string.IsNullOrWhiteSpace(update.Detail) ? update.Stage : $"{update.Stage}: {update.Detail}";
                Progress = update.Total == 0 ? 12 : 12 + 78d * update.Completed / update.Total;
            });
            var result = await _indexer.IndexAsync(Installation, progress, token);
            Progress = 92;
            Status = "Identificando banco de dados...";
            Populate(result);
            try { await new SqliteIndexStore().SaveAsync(result, token); }
            catch (Exception ex) when (ex is IOException or Microsoft.Data.Sqlite.SqliteException)
            {
                Issues.Add(new IntegrityIssue("CACHE_WRITE", $"Índice SQLite não salvo: {ex.Message}"));
            }
            await _recentStore.AddAsync(Installation.RootPath, token);
            if (!RecentInstallations.Contains(Installation.RootPath, StringComparer.OrdinalIgnoreCase))
                RecentInstallations.Insert(0, Installation.RootPath);
            Progress = 100;
            Status = "Finalizando — leitura somente, nenhum arquivo do jogo foi alterado.";
            SelectedPage = "Visão geral";
        }
        catch (OperationCanceledException)
        {
            Status = "Operação cancelada com segurança.";
        }
        catch (Exception ex)
        {
            Status = $"Falha ao carregar: {ex.Message}";
            Issues.Add(new IntegrityIssue("LOAD_ERROR", ex.Message, SupportStatus.Invalid));
        }
        finally
        {
            IsLoading = false;
            _loadCancellation?.Dispose();
            _loadCancellation = null;
        }
    }

    private async Task ExtractAsync(object? parameter)
    {
        if (parameter is not CpkEntryRow row) return;
        var dialog = new SaveFileDialog { Title = "Extrair arquivo CPK", FileName = Path.GetFileName(row.Entry.InternalPath) };
        if (dialog.ShowDialog() != true) return;
        try
        {
            await _reader.ExtractEntryAsync(row.Entry, dialog.FileName);
            Status = $"Arquivo extraído: {row.Entry.InternalPath}";
        }
        catch (Exception ex)
        {
            Status = $"Falha na extração: {ex.Message}";
        }
    }

    private async Task CheckForUpdatesAsync(bool automatic)
    {
        if (IsCheckingUpdates) return;
        IsCheckingUpdates = true;
        try
        {
            var current = SemanticVersion.Parse(AppMetadata.Version);
            var result = await _updateService.CheckLatestAsync(current);
            _availableUpdate = result.AvailableUpdate;
            UpdateStatus = result.Message;
            OnPropertyChanged(nameof(IsUpdateAvailable));
            OnPropertyChanged(nameof(LatestVersionText));
            OnPropertyChanged(nameof(LatestReleaseNotes));
            OnPropertyChanged(nameof(LatestInstallerSizeText));
            _settings.LastUpdateCheckUtc = DateTimeOffset.UtcNow;
            await SaveSettingsAsync();
        }
        catch (OperationCanceledException)
        {
            if (!automatic) UpdateStatus = "Verificação cancelada.";
        }
        catch (Exception ex)
        {
            UpdateStatus = $"Falha na verificação de atualização: {ex.Message}";
        }
        finally
        {
            IsCheckingUpdates = false;
        }
    }

    private async Task DownloadAndLaunchUpdateAsync()
    {
        if (_availableUpdate is null || IsUpdating) return;
        IsUpdating = true;
        UpdateProgress = 0;
        var update = _availableUpdate;
        var startedAt = DateTimeOffset.UtcNow;
        try
        {
            UpdateStatus = $"Baixando v{update.Version}...";
            var fileName = $"PES2017DatabaseEditor-Setup-v{update.Version}-win-x64.exe";
            var destination = Path.Combine(AppDataPaths.UpdatesDirectory, fileName);
            var progress = new Progress<DownloadProgress>(value => UpdateProgress = value.Percentage ?? 0);
            await _updateService.DownloadInstallerAsync(update, destination, progress);
            await _updateHistory.AppendAsync(new UpdateHistoryEntry(update.Version.ToString(), startedAt, true, destination));
            if (UpdateProcessLauncher.TryLaunch(destination))
            {
                UpdateStatus = "Instalador validado. O aplicativo será fechado para atualizar com segurança.";
                await Task.Delay(250);
                System.Windows.Application.Current.Shutdown(0);
            }
            else
            {
                UpdateStatus = $"Instalador validado e salvo em: {destination}";
            }
        }
        catch (Exception ex)
        {
            await _updateHistory.AppendAsync(new UpdateHistoryEntry(update.Version.ToString(), startedAt, false, ex.Message));
            UpdateStatus = $"Falha no download: {ex.Message}";
        }
        finally
        {
            IsUpdating = false;
        }
    }

    private async Task SaveSettingsAsync() => await _settingsStore.SaveAsync(_settings);

    private static void OpenUrl(object? parameter)
    {
        if (parameter is not string url || !Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps) return;
        Process.Start(new ProcessStartInfo(uri.ToString()) { UseShellExecute = true });
    }

    private void Populate(CpkIndexResult result)
    {
        Archives.Clear();
        Entries.Clear();
        Issues.Clear();
        foreach (var archive in result.Archives) Archives.Add(new CpkArchiveRow(archive));
        foreach (var entry in result.Entries) Entries.Add(new CpkEntryRow(entry));
        foreach (var warning in result.Warnings) Issues.Add(new IntegrityIssue("CPK", warning));
        var totalBytes = result.Archives.Sum(x => x.FileSize);
        var compatibility = result.Archives.Count == 0 ? SupportStatus.NotSupported :
            result.Archives.Any(x => x.FailureReason is not null) ? SupportStatus.PartiallySupported : SupportStatus.Supported;
        Facts = new DashboardFacts(result.Archives.Count, result.Entries.Count, Installation?.ExecutableVersion,
            totalBytes, result.Warnings.Count, null, compatibility, Issues.ToArray());
        OnPropertyChanged(nameof(IndexedCountText));
        OnPropertyChanged(nameof(CpkCountText));
        OnPropertyChanged(nameof(TotalSizeText));
        OnPropertyChanged(nameof(CompatibilityText));
        RefreshEntries();
    }

    private void RefreshEntries()
    {
        var filter = FilterText.Trim();
        foreach (var row in Entries)
            row.IsVisible = string.IsNullOrWhiteSpace(filter) || row.Entry.InternalPath.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    private void Cancel() => _loadCancellation?.Cancel();

    private void RaiseCommands()
    {
        (LoadCommand as AsyncCommand)?.RaiseCanExecuteChanged();
        (CancelCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (CheckUpdatesCommand as AsyncCommand)?.RaiseCanExecuteChanged();
        (UpdateNowCommand as AsyncCommand)?.RaiseCanExecuteChanged();
    }

    private static string FormatBytes(long value)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var size = (double)value;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1) { size /= 1024; unit++; }
        return $"{size:0.##} {units[unit]}";
    }
}

public sealed record NavigationItem(string Title, string Icon);
public sealed record CpkArchiveRow(CpkArchive Archive)
{
    public string Name => Archive.FileName;
    public string Status => Archive.FailureReason is null ? "Indexado" : "Parcial";
    public string Size => Archive.FileSize.ToString("N0");
    public string Entries => Archive.Entries.Count.ToString("N0");
}

public sealed class CpkEntryRow : NotifyPropertyChanged
{
    private bool _isVisible = true;
    public CpkEntryRow(CpkEntry entry) => Entry = entry;
    public CpkEntry Entry { get; }
    public string ArchiveName => Path.GetFileName(Entry.ArchivePath);
    public string InternalPath => Entry.InternalPath;
    public string StoredSize => Entry.StoredSize.ToString("N0");
    public string OriginalSize => Entry.OriginalSize.ToString("N0");
    public string Codec => Entry.Codec.ToString();
    public bool IsVisible { get => _isVisible; set => Set(ref _isVisible, value); }
}

public abstract class NotifyPropertyChanged : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value; OnPropertyChanged(name); return true;
    }
    protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}

public sealed class RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => execute(parameter);
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public sealed class AsyncCommand(Func<Task> execute, Func<bool>? canExecute = null) : ICommand
{
    private bool _running;
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => !_running && (canExecute?.Invoke() ?? true);
    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;
        try { _running = true; RaiseCanExecuteChanged(); await execute(); }
        finally { _running = false; RaiseCanExecuteChanged(); }
    }
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public sealed class AsyncCommand<T>(Func<T?, Task> execute) : ICommand
{
    public event EventHandler? CanExecuteChanged { add { } remove { } }
    public bool CanExecute(object? parameter) => true;
    public async void Execute(object? parameter) => await execute((T?)parameter);
}
