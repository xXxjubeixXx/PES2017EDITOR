using PES2017Editor.Infrastructure;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace PES2017Editor.Tests;

public sealed class Phase02Tests
{
    [Fact]
    public void SemanticVersionOrdersStableVersionsAbovePrereleases()
    {
        var stable = SemanticVersion.Parse("v0.2.0");
        var previous = SemanticVersion.Parse("0.1.0");
        var preview = SemanticVersion.Parse("0.2.0-alpha.1");

        Assert.True(stable.CompareTo(previous) > 0);
        Assert.True(stable.CompareTo(preview) > 0);
        Assert.Equal("0.2.0-alpha.1", preview.ToString());
    }

    [Fact]
    public async Task SettingsStorePersistsPortableUserPreferences()
    {
        var root = Path.Combine(Path.GetTempPath(), "pes2017-editor-settings-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(root, "settings.json");
        try
        {
            var store = new EditorSettingsStore(path);
            await store.SaveAsync(new EditorSettings
            {
                AutoCheckForUpdates = false,
                BackupsDirectory = "D:\\PES-Backups",
                ProjectsDirectory = "D:\\PES-Projects"
            });
            var loaded = await store.LoadAsync();
            Assert.False(loaded.AutoCheckForUpdates);
            Assert.Equal("D:\\PES-Backups", loaded.BackupsDirectory);
            Assert.Equal("D:\\PES-Projects", loaded.ProjectsDirectory);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task UpdateServiceReadsManifestAndValidatesDownloadedHash()
    {
        var bytes = Encoding.UTF8.GetBytes("setup");
        var hash = Convert.ToHexString(SHA256.HashData(bytes));
        var installerName = "PES2017DatabaseEditor-Setup-v0.2.1-win-x64.exe";
        var handler = new StubHttpMessageHandler(request => request.RequestUri?.AbsolutePath.EndsWith("update-manifest.json", StringComparison.OrdinalIgnoreCase) == true
            ? JsonResponse($"{{\"version\":\"0.2.1\",\"installer\":{{\"url\":\"https://github.com/xXxjubeixXx/PES2017EDITOR/releases/download/v0.2.1/{installerName}\",\"size\":{bytes.Length},\"sha256\":\"{hash}\"}}}}")
            : request.RequestUri?.AbsolutePath.EndsWith(installerName, StringComparison.OrdinalIgnoreCase) == true
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) }
                : JsonResponse($"{{\"tag_name\":\"v0.2.1\",\"name\":\"Test\",\"body\":\"Notas\",\"draft\":false,\"prerelease\":false,\"html_url\":\"https://github.com/xXxjubeixXx/PES2017EDITOR/releases/tag/v0.2.1\",\"published_at\":\"2026-11-24T12:00:00Z\",\"assets\":[{{\"name\":\"{installerName}\",\"browser_download_url\":\"https://github.com/xXxjubeixXx/PES2017EDITOR/releases/download/v0.2.1/{installerName}\",\"size\":{bytes.Length},\"digest\":\"sha256:{hash}\"}},{{\"name\":\"update-manifest.json\",\"browser_download_url\":\"https://github.com/xXxjubeixXx/PES2017EDITOR/releases/download/v0.2.1/update-manifest.json\",\"size\":1}}]}}"));
        var service = new GitHubUpdateService(new HttpClient(handler));
        var result = await service.CheckLatestAsync(SemanticVersion.Parse("0.2.0"));
        Assert.NotNull(result.AvailableUpdate);
        Assert.Equal("0.2.1", result.AvailableUpdate!.Version.ToString());
        Assert.Equal(hash, result.AvailableUpdate.Sha256);

        var root = Path.Combine(Path.GetTempPath(), "pes2017-editor-update-" + Guid.NewGuid().ToString("N"));
        var destination = Path.Combine(root, "setup.exe");
        try
        {
            await service.DownloadInstallerAsync(result.AvailableUpdate, destination);
            Assert.Equal(bytes, await File.ReadAllBytesAsync(destination));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(handler(request));
    }
}
