using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using PES2017Editor.Core;

namespace PES2017Editor.Infrastructure;

public readonly record struct SemanticVersion(int Major, int Minor, int Patch, string? PreRelease = null) : IComparable<SemanticVersion>
{
    public static SemanticVersion Parse(string value)
    {
        if (!TryParse(value, out var version))
            throw new FormatException($"Versão SemVer inválida: {value}");
        return version;
    }

    public static bool TryParse(string? value, out SemanticVersion version)
    {
        version = default;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var normalized = value.Trim().TrimStart('v', 'V').Split('+', 2)[0];
        var parts = normalized.Split('-', 2);
        var numbers = parts[0].Split('.');
        if (numbers.Length != 3 ||
            !int.TryParse(numbers[0], NumberStyles.None, CultureInfo.InvariantCulture, out var major) ||
            !int.TryParse(numbers[1], NumberStyles.None, CultureInfo.InvariantCulture, out var minor) ||
            !int.TryParse(numbers[2], NumberStyles.None, CultureInfo.InvariantCulture, out var patch) ||
            major < 0 || minor < 0 || patch < 0)
            return false;
        var preRelease = parts.Length == 2 && parts[1].Length > 0 ? parts[1] : null;
        version = new SemanticVersion(major, minor, patch, preRelease);
        return true;
    }

    public int CompareTo(SemanticVersion other)
    {
        var result = Major.CompareTo(other.Major);
        if (result != 0) return result;
        result = Minor.CompareTo(other.Minor);
        if (result != 0) return result;
        result = Patch.CompareTo(other.Patch);
        if (result != 0) return result;
        if (PreRelease is null && other.PreRelease is null) return 0;
        if (PreRelease is null) return 1;
        if (other.PreRelease is null) return -1;
        var left = PreRelease.Split('.');
        var right = other.PreRelease.Split('.');
        for (var i = 0; i < Math.Max(left.Length, right.Length); i++)
        {
            if (i >= left.Length) return -1;
            if (i >= right.Length) return 1;
            var leftNumber = int.TryParse(left[i], NumberStyles.None, CultureInfo.InvariantCulture, out var leftValue);
            var rightNumber = int.TryParse(right[i], NumberStyles.None, CultureInfo.InvariantCulture, out var rightValue);
            if (leftNumber && rightNumber)
            {
                var numericResult = leftValue.CompareTo(rightValue);
                if (numericResult != 0) return numericResult;
            }
            else if (leftNumber != rightNumber)
            {
                return leftNumber ? -1 : 1;
            }
            else
            {
                var textResult = string.CompareOrdinal(left[i], right[i]);
                if (textResult != 0) return textResult;
            }
        }
        return 0;
    }

    public override string ToString() => $"{Major}.{Minor}.{Patch}{(PreRelease is null ? string.Empty : $"-{PreRelease}")}";
}

public sealed record UpdateInfo(
    SemanticVersion Version,
    string ReleaseName,
    string ReleaseNotes,
    DateTimeOffset? PublishedAt,
    long InstallerSize,
    string InstallerUrl,
    string? Sha256,
    string ReleaseUrl);

public sealed record UpdateCheckResult(
    SemanticVersion CurrentVersion,
    UpdateInfo? AvailableUpdate,
    string Message,
    bool IsError = false);

public sealed record DownloadProgress(long BytesReceived, long? TotalBytes)
{
    public double? Percentage => TotalBytes is > 0 ? BytesReceived * 100d / TotalBytes.Value : null;
}

public sealed class GitHubUpdateService
{
    private const string ReleasesEndpoint = "https://api.github.com/repos/xXxjubeixXx/PES2017EDITOR/releases/latest";
    private readonly HttpClient _httpClient;

    public GitHubUpdateService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("PES2017DatabaseEditor", AppMetadata.Version));
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    }

    public async Task<UpdateCheckResult> CheckLatestAsync(SemanticVersion currentVersion, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync(ReleasesEndpoint, cancellationToken);
            response.EnsureSuccessStatusCode();
            await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
            var root = document.RootElement;
            if (root.TryGetProperty("draft", out var draft) && draft.GetBoolean() ||
                root.TryGetProperty("prerelease", out var prerelease) && prerelease.GetBoolean())
                return new UpdateCheckResult(currentVersion, null, "Nenhuma release estável disponível.");

            var tag = root.GetProperty("tag_name").GetString();
            if (!SemanticVersion.TryParse(tag, out var latestVersion))
                return new UpdateCheckResult(currentVersion, null, "A release mais recente possui versão inválida.", true);
            if (latestVersion.CompareTo(currentVersion) <= 0)
                return new UpdateCheckResult(currentVersion, null, "Você já está usando a versão mais recente.");

            var assets = root.TryGetProperty("assets", out var assetArray)
                ? assetArray.EnumerateArray().ToArray()
                : Array.Empty<JsonElement>();
            var installer = assets.FirstOrDefault(asset =>
                asset.TryGetProperty("name", out var name) &&
                name.GetString()?.EndsWith("-win-x64.exe", StringComparison.OrdinalIgnoreCase) == true &&
                name.GetString()?.Contains("Setup", StringComparison.OrdinalIgnoreCase) == true);
            if (installer.ValueKind == JsonValueKind.Undefined)
                return new UpdateCheckResult(currentVersion, null, "A release não possui instalador Windows x64.", true);

            var installerUrl = installer.GetProperty("browser_download_url").GetString()!;
            var installerSize = installer.TryGetProperty("size", out var size) ? size.GetInt64() : 0;
            var sha256 = installer.TryGetProperty("digest", out var digest) ? NormalizeSha256(digest.GetString()) : null;
            var manifestAsset = assets.FirstOrDefault(asset =>
                asset.TryGetProperty("name", out var name) &&
                name.GetString()?.Equals("update-manifest.json", StringComparison.OrdinalIgnoreCase) == true);
            if (manifestAsset.ValueKind != JsonValueKind.Undefined)
            {
                try
                {
                    var manifestUrl = manifestAsset.GetProperty("browser_download_url").GetString();
                    if (manifestUrl is not null && IsAllowedGithubUrl(manifestUrl))
                    {
                        using var manifest = JsonDocument.Parse(await _httpClient.GetStringAsync(manifestUrl, cancellationToken));
                        var manifestRoot = manifest.RootElement;
                        if (manifestRoot.TryGetProperty("version", out var manifestVersion) &&
                            SemanticVersion.TryParse(manifestVersion.GetString(), out var parsedManifestVersion) &&
                            parsedManifestVersion.CompareTo(latestVersion) == 0 &&
                            manifestRoot.TryGetProperty("installer", out var manifestInstaller))
                        {
                            installerUrl = manifestInstaller.GetProperty("url").GetString() ?? installerUrl;
                            installerSize = manifestInstaller.TryGetProperty("size", out var manifestSize) ? manifestSize.GetInt64() : installerSize;
                            sha256 = manifestInstaller.TryGetProperty("sha256", out var manifestHash)
                                ? NormalizeSha256(manifestHash.GetString())
                                : sha256;
                        }
                    }
                }
                catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException)
                {
                    // The release API asset remains a valid fallback when the optional manifest is unavailable.
                }
            }
            var releaseUrl = root.GetProperty("html_url").GetString()!;
            var publishedAt = root.TryGetProperty("published_at", out var published) && published.ValueKind != JsonValueKind.Null
                ? (DateTimeOffset?)published.GetDateTimeOffset()
                : null;
            var update = new UpdateInfo(latestVersion,
                root.TryGetProperty("name", out var releaseName) ? releaseName.GetString() ?? tag! : tag!,
                root.TryGetProperty("body", out var body) ? body.GetString() ?? string.Empty : string.Empty,
                publishedAt, installerSize, installerUrl, sha256, releaseUrl);
            return new UpdateCheckResult(currentVersion, update, $"Atualização disponível: {latestVersion}.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            return new UpdateCheckResult(currentVersion, null, $"Não foi possível consultar o GitHub: {ex.Message}", true);
        }
    }

    public async Task<string> DownloadInstallerAsync(UpdateInfo update, string destinationPath,
        IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        if (!IsAllowedGithubUrl(update.InstallerUrl))
            throw new InvalidDataException("A URL do instalador não pertence a uma origem GitHub permitida.");
        var destination = Path.GetFullPath(destinationPath);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        var temporary = destination + ".download";
        try
        {
            using var response = await _httpClient.GetAsync(update.InstallerUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            var total = response.Content.Headers.ContentLength ?? update.InstallerSize;
            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var target = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None,
                128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[128 * 1024];
            long received = 0;
            int read;
            while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                hash.AppendData(buffer, 0, read);
                received += read;
                progress?.Report(new DownloadProgress(received, total > 0 ? total : null));
            }
            await target.FlushAsync(cancellationToken);
            await target.DisposeAsync();
            if (update.InstallerSize > 0 && received != update.InstallerSize)
                throw new InvalidDataException($"Tamanho do instalador divergente: esperado {update.InstallerSize}, recebido {received}.");
            var actualHash = Convert.ToHexString(hash.GetHashAndReset());
            if (!string.IsNullOrWhiteSpace(update.Sha256) && !actualHash.Equals(update.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"SHA-256 do instalador inválido: esperado {update.Sha256}, recebido {actualHash}.");
            File.Move(temporary, destination, overwrite: true);
            return destination;
        }
        catch
        {
            if (File.Exists(temporary)) File.Delete(temporary);
            throw;
        }
    }

    private static bool IsAllowedGithubUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            return false;
        return uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.EndsWith(".github.com", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.EndsWith(".githubusercontent.com", StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeSha256(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Replace("sha256:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
}
