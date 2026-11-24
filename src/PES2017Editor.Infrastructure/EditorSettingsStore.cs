using System.Text.Json;

namespace PES2017Editor.Infrastructure;

public sealed class EditorSettings
{
    public bool AutoCheckForUpdates { get; set; } = true;
    public int UpdateCheckIntervalHours { get; set; } = 24;
    public DateTimeOffset? LastUpdateCheckUtc { get; set; }
    public string? BackupsDirectory { get; set; }
    public string? ProjectsDirectory { get; set; }
    public string? LargeFilesDirectory { get; set; }
}

public sealed class EditorSettingsStore
{
    private readonly string _path;

    public EditorSettingsStore(string? path = null) => _path = path ?? AppDataPaths.SettingsPath;

    public async Task<EditorSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path)) return new EditorSettings();
        try
        {
            await using var stream = File.OpenRead(_path);
            return await JsonSerializer.DeserializeAsync<EditorSettings>(stream, cancellationToken: cancellationToken)
                ?? new EditorSettings();
        }
        catch (JsonException)
        {
            return new EditorSettings();
        }
    }

    public async Task SaveAsync(EditorSettings settings, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var temporaryPath = _path + ".tmp";
        await using (var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, settings, new JsonSerializerOptions { WriteIndented = true }, cancellationToken);
        }
        File.Move(temporaryPath, _path, overwrite: true);
    }
}
