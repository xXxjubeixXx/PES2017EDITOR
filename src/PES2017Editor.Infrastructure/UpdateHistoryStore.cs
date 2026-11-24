using System.Text.Json;

namespace PES2017Editor.Infrastructure;

public sealed record UpdateHistoryEntry(
    string Version,
    DateTimeOffset StartedAtUtc,
    bool Succeeded,
    string? Details);

public sealed class UpdateHistoryStore
{
    private readonly string _path;

    public UpdateHistoryStore(string? path = null) => _path = path ?? AppDataPaths.UpdateHistoryPath;

    public async Task<IReadOnlyList<UpdateHistoryEntry>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path)) return Array.Empty<UpdateHistoryEntry>();
        try
        {
            await using var stream = File.OpenRead(_path);
            return await JsonSerializer.DeserializeAsync<List<UpdateHistoryEntry>>(stream, cancellationToken: cancellationToken)
                ?? [];
        }
        catch (JsonException)
        {
            return Array.Empty<UpdateHistoryEntry>();
        }
    }

    public async Task AppendAsync(UpdateHistoryEntry entry, CancellationToken cancellationToken = default)
    {
        var entries = (await LoadAsync(cancellationToken)).Prepend(entry).Take(20).ToArray();
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        await using var stream = File.Create(_path);
        await JsonSerializer.SerializeAsync(stream, entries, new JsonSerializerOptions { WriteIndented = true }, cancellationToken);
    }
}
