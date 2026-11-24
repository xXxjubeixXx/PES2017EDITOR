using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
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
    private readonly PesDatabaseReader _databaseReader = new();
    private readonly ExperimentalCpkWriter _experimentalWriter = new();
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
    private CpkIndexResult? _indexResult;
    private PesDatabaseSnapshot? _database;
    private string _playerFilterText = string.Empty;
    private PesPlayerRow? _selectedPlayer;
    private string _databaseStatus = "Os BINs de jogadores, times e competições ainda não foram carregados.";
    private string _experimentalStatus = "Nenhuma edição experimental foi gerada.";
    private readonly bool _safeMode;

    public MainViewModel(bool safeMode = false)
    {
        _safeMode = safeMode;
        BrowseCommand = new AsyncCommand(BrowseAsync);
        LoadCommand = new AsyncCommand(LoadAsync, () => !IsLoading && !string.IsNullOrWhiteSpace(SelectedPath));
        CancelCommand = new RelayCommand(_ => Cancel(), _ => IsLoading);
        ExtractCommand = new AsyncCommand<object?>(ExtractAsync);
        SavePlayerCommand = new AsyncCommand<object?>(SavePlayerAsync);
        DiscardPlayerCommand = new RelayCommand(parameter => (parameter as PesPlayerRow)?.Discard());
        CheckUpdatesCommand = new AsyncCommand(() => CheckForUpdatesAsync(automatic: false), () => !IsCheckingUpdates && !IsUpdating);
        UpdateNowCommand = new AsyncCommand(DownloadAndLaunchUpdateAsync, () => IsUpdateAvailable && !IsUpdating && !IsCheckingUpdates);
        OpenRepositoryCommand = new RelayCommand(parameter => OpenUrl(parameter));
        OpenLogsCommand = new RelayCommand(_ => OpenLogs());
        CopyLogPathCommand = new RelayCommand(_ => CopyLogPath());
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
    public ObservableCollection<PesPlayerRow> Players { get; } = [];
    public ObservableCollection<PesTeamRow> Teams { get; } = [];
    public ObservableCollection<PesCompetitionRow> Competitions { get; } = [];
    public ObservableCollection<IntegrityIssue> Issues { get; } = [];
    public ObservableCollection<BinaryField> PreviewFields { get; } = [];

    public ICommand BrowseCommand { get; }
    public ICommand LoadCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ExtractCommand { get; }
    public ICommand SavePlayerCommand { get; }
    public ICommand DiscardPlayerCommand { get; }
    public ICommand CheckUpdatesCommand { get; }
    public ICommand UpdateNowCommand { get; }
    public ICommand OpenRepositoryCommand { get; }
    public ICommand OpenLogsCommand { get; }
    public ICommand CopyLogPathCommand { get; }

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
    public string PlayerFilterText
    {
        get => _playerFilterText;
        set { if (Set(ref _playerFilterText, value)) RefreshPlayers(); }
    }
    public PesPlayerRow? SelectedPlayer
    {
        get => _selectedPlayer;
        set => Set(ref _selectedPlayer, value);
    }
    public string DatabaseStatus { get => _databaseStatus; private set => Set(ref _databaseStatus, value); }
    public string ExperimentalStatus { get => _experimentalStatus; private set => Set(ref _experimentalStatus, value); }
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
    public string StartupLogPathText => EditorLogger.Default.StartupLogPath;
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
    public string PlayerCountText => _database is null ? "Não identificado" : _database.Players.Count.ToString("N0");
    public string TeamCountText => _database is null ? "Não identificado" : _database.Teams.Count.ToString("N0");
    public string CompetitionCountText => _database is null ? "Não identificado" : _database.Competitions.Count.ToString("N0");
    public string AssignmentCountText => _database is null ? "Não identificado" : _database.Assignments.Count.ToString("N0");

    public async Task InitializeAsync()
    {
        if (_safeMode)
        {
            Status = "Modo seguro ativo — configurações, cache e atualização automática foram ignorados.";
            EditorLogger.Default.WriteStartup("Modo seguro ativo; serviços persistentes ignorados.");
            return;
        }
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

    private void OpenLogs()
    {
        try
        {
            Directory.CreateDirectory(AppDataPaths.LogsDirectory);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{AppDataPaths.LogsDirectory}\"") { UseShellExecute = true });
        }
        catch (Exception exception) { EditorLogger.Default.WriteApplication("Não foi possível abrir a pasta de logs.", exception); }
    }

    private void CopyLogPath()
    {
        try { System.Windows.Clipboard.SetText(EditorLogger.Default.StartupLogPath); }
        catch (Exception exception) { EditorLogger.Default.WriteApplication("Não foi possível copiar o caminho do log.", exception); }
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
            Status = "Carregando informações reais de jogadores, times e competições...";
            _indexResult = result;
            _database = await _databaseReader.ReadAsync(result, _reader, token);
            PopulateDatabase(_database);
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

    private async Task SavePlayerAsync(object? parameter)
    {
        if (parameter is not PesPlayerRow row || !row.IsModified)
        {
            ExperimentalStatus = "Selecione uma alteração de nome antes de salvar.";
            return;
        }
        var document = _database?.PlayerDocument;
        if (document is null)
        {
            ExperimentalStatus = "Player.bin não está disponível em uma TOC legível.";
            return;
        }

        try
        {
            var payload = PesDatabaseParser.ReplacePlayerName(document.Wesys.Payload, row.RecordIndex, row.EditedName);
            var replacement = WesysCodec.Encode(document.Wesys, payload);
            var sourceHash = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(document.Entry.ArchivePath)));
            var backupDirectory = _settings.BackupsDirectory ?? AppDataPaths.BackupsDirectory;
            Directory.CreateDirectory(backupDirectory);
            var backupPath = Path.Combine(backupDirectory,
                $"{Path.GetFileNameWithoutExtension(document.Entry.ArchivePath)}-{DateTimeOffset.Now:yyyyMMdd-HHmmss}-{sourceHash[..12]}.cpk");
            if (!File.Exists(backupPath))
                File.Copy(document.Entry.ArchivePath, backupPath, overwrite: false);

            var root = _settings.ProjectsDirectory ?? AppDataPaths.ProjectsDirectory;
            var outputDirectory = Path.Combine(root, "experimental");
            Directory.CreateDirectory(outputDirectory);
            var fileName = $"dt10_win-player-{row.Id}-{DateTimeOffset.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.cpk";
            var outputPath = Path.Combine(outputDirectory, fileName);
            var result = await _experimentalWriter.CreateLayoutPreservingCopyAsync(document.Entry, replacement, outputPath);

            var reopened = await _reader.ReadAsync(result.OutputPath);
            var reopenedEntry = reopened.Entries.FirstOrDefault(x =>
                x.InternalPath.Equals(document.Entry.InternalPath, StringComparison.OrdinalIgnoreCase));
            if (reopenedEntry is null) throw new InvalidDataException("O CPK experimental não reabriu a entrada modificada.");
            var reopenedStored = await _reader.ReadEntryAsync(reopenedEntry);
            var reopenedPayload = WesysCodec.Decode(reopenedStored).Payload;
            var reopenedPlayer = PesDatabaseParser.ParsePlayers(reopenedPayload).ElementAtOrDefault(row.RecordIndex);
            if (reopenedPlayer is null || !string.Equals(reopenedPlayer.Name, row.EditedName, StringComparison.Ordinal))
                throw new InvalidDataException("A validação após a reabertura não encontrou o nome modificado.");
            if (!EqualOutside(document.Wesys.Payload, payload, row.RecordIndex * PesPlayerRecord.RecordSize + PesPlayerRecord.NameOffset, PesPlayerRecord.NameCapacity))
                throw new InvalidDataException("A serialização alterou bytes fora do campo de nome permitido.");

            var originalHash = sourceHash;
            var generatedHash = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(result.OutputPath)));
            row.MarkSaved();
            ExperimentalStatus = $"CPK experimental validado: {result.OutputPath} · SHA-256 origem {originalHash[..16]}… · cópia {generatedHash[..16]}….";
            Status = "Edição aplicada somente à cópia experimental; a instalação original permaneceu intacta.";
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            ExperimentalStatus = $"Edição não aplicada: {ex.Message}";
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

    private void PopulateDatabase(PesDatabaseSnapshot database)
    {
        Players.Clear();
        Teams.Clear();
        Competitions.Clear();
        var teamById = database.Teams.ToDictionary(x => x.Id);
        foreach (var player in database.Players)
        {
            database.PlayerToTeam.TryGetValue(player.Id, out var teamId);
            Players.Add(new PesPlayerRow(player, teamId, teamById.TryGetValue(teamId, out var team) ? team.Name : string.Empty));
        }
        foreach (var team in database.Teams) Teams.Add(new PesTeamRow(team));
        foreach (var competition in database.Competitions) Competitions.Add(new PesCompetitionRow(competition));
        foreach (var warning in database.Warnings) Issues.Add(new IntegrityIssue("DATABASE", warning));
        DatabaseStatus = $"{Players.Count:N0} jogadores · {Teams.Count:N0} times · {Competitions.Count:N0} competições · {database.Assignments.Count:N0} associações lidas.";
        OnPropertyChanged(nameof(PlayerCountText));
        OnPropertyChanged(nameof(TeamCountText));
        OnPropertyChanged(nameof(CompetitionCountText));
        OnPropertyChanged(nameof(AssignmentCountText));
        RefreshPlayers();
    }

    private void RefreshEntries()
    {
        var filter = FilterText.Trim();
        foreach (var row in Entries)
            row.IsVisible = string.IsNullOrWhiteSpace(filter) || row.Entry.InternalPath.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    private void RefreshPlayers()
    {
        var filter = PlayerFilterText.Trim();
        foreach (var row in Players)
            row.IsVisible = string.IsNullOrWhiteSpace(filter) ||
                row.Id.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                row.EditedName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                row.ShirtName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                row.TeamName.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    private static bool EqualOutside(ReadOnlySpan<byte> original, ReadOnlySpan<byte> updated, int changedOffset, int changedLength)
    {
        if (original.Length != updated.Length || changedOffset < 0 || changedOffset > original.Length - changedLength) return false;
        return original[..changedOffset].SequenceEqual(updated[..changedOffset]) &&
               original[(changedOffset + changedLength)..].SequenceEqual(updated[(changedOffset + changedLength)..]);
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

public sealed class PesPlayerRow : NotifyPropertyChanged
{
    private string _editedName;
    private bool _isVisible = true;
    private bool _isSaved;
    public PesPlayerRow(PesPlayerRecord record, uint teamId, string teamName)
    {
        RecordIndex = record.Index; Id = record.Id; OriginalName = record.Name; _editedName = record.Name;
        ShirtName = record.ShirtName; TeamId = teamId; TeamName = teamName;
    }
    public int RecordIndex { get; }
    public uint Id { get; }
    public string OriginalName { get; private set; }
    public string ShirtName { get; }
    public uint TeamId { get; }
    public string TeamName { get; }
    public string EditedName { get => _editedName; set { if (Set(ref _editedName, value)) { _isSaved = false; OnPropertyChanged(nameof(IsModified)); OnPropertyChanged(nameof(State)); } } }
    public bool IsModified => !string.Equals(OriginalName, EditedName, StringComparison.Ordinal);
    public bool IsVisible { get => _isVisible; set => Set(ref _isVisible, value); }
    public string State => _isSaved ? "Validado" : IsModified ? "Alterado" : "Original";
    public void Discard() { EditedName = OriginalName; _isSaved = false; OnPropertyChanged(nameof(State)); }
    public void MarkSaved() { OriginalName = EditedName; _isSaved = true; OnPropertyChanged(nameof(OriginalName)); OnPropertyChanged(nameof(IsModified)); OnPropertyChanged(nameof(State)); }
}

public sealed record PesTeamRow(PesTeamRecord Record)
{
    public uint Id => Record.Id;
    public string Name => Record.Name;
    public int Index => Record.Index;
    public string Evidence => Record.NameEvidence.ToString();
}

public sealed record PesCompetitionRow(PesCompetitionRecord Record)
{
    public int Index => Record.Index;
    public string Name => Record.Name;
    public string Evidence => Record.Evidence.ToString();
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
