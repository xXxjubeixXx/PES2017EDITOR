using System.Text.Json;

namespace PES2017Editor.Infrastructure;

public sealed class RecentInstallationsStore
{
    private readonly string _path;

    public RecentInstallationsStore(string? path = null)
    {
        _path = path ?? AppDataPaths.RecentInstallationsPath;
    }

    public async Task<IReadOnlyList<string>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path)) return Array.Empty<string>();
        try
        {
            await using var stream = File.OpenRead(_path);
            var values = await JsonSerializer.DeserializeAsync<List<string>>(stream, cancellationToken: cancellationToken);
            return (values ?? []).Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase).Take(10).ToArray();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    public async Task SaveAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var values = paths.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase).Take(10).ToArray();
        await using var stream = File.Create(_path);
        await JsonSerializer.SerializeAsync(stream, values, new JsonSerializerOptions { WriteIndented = true }, cancellationToken);
    }

    public async Task AddAsync(string path, CancellationToken cancellationToken = default)
    {
        var current = await LoadAsync(cancellationToken);
        await SaveAsync(new[] { Path.GetFullPath(path) }.Concat(current), cancellationToken);
    }
}
