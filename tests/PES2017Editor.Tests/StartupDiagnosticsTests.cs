using PES2017Editor.Infrastructure;
using Xunit;

namespace PES2017Editor.Tests;

public sealed class StartupDiagnosticsTests
{
    [Fact]
    public void ProgressBindingIsExplicitlyOneWay()
    {
        var xaml = File.ReadAllText(FindWorkspaceFile("src/PES2017Editor.App/MainWindow.xaml"));

        Assert.Contains("Value=\"{Binding Progress, Mode=OneWay}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Value=\"{Binding UpdateProgress, Mode=OneWay}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("StartupUri=\"MainWindow.xaml\"", File.ReadAllText(FindWorkspaceFile("src/PES2017Editor.App/App.xaml")), StringComparison.Ordinal);
    }

    [Fact]
    public void LoggerWritesSeparateStartupAndCrashFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "pes2017-editor-logs-" + Guid.NewGuid().ToString("N"));
        try
        {
            var logger = new EditorLogger(root);
            logger.WriteStartupContext(true, ["--safe-mode"]);
            logger.WriteCrash("Teste controlado", new InvalidOperationException("falha sintética"));

            Assert.Contains("safeMode=True", File.ReadAllText(logger.StartupLogPath), StringComparison.Ordinal);
            Assert.Contains("falha sintética", File.ReadAllText(logger.CrashLogPath), StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static string FindWorkspaceFile(string relative)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, relative.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(candidate)) return candidate;
            current = current.Parent;
        }

        throw new FileNotFoundException($"Arquivo do workspace não encontrado: {relative}");
    }
}
