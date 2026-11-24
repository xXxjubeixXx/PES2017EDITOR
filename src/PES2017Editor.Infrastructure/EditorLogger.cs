using System.Text;
using PES2017Editor.Core;

namespace PES2017Editor.Infrastructure;

/// <summary>Logger that is safe to use before the WPF visual tree exists.</summary>
public sealed class EditorLogger
{
    private const long MaxLogBytes = 1_048_576;
    private readonly string _rootDirectory;
    private readonly object _gate = new();

    public EditorLogger(string? rootDirectory = null) => _rootDirectory = rootDirectory ?? AppDataPaths.LogsDirectory;

    public static EditorLogger Default { get; } = new();
    public string RootDirectory => _rootDirectory;
    public string StartupLogPath => GetPath("startup.log");
    public string ApplicationLogPath => GetPath("application.log");
    public string CrashLogPath => GetPath("crash.log");
    public string UpdateLogPath => GetPath("update.log");

    public void WriteStartup(string message, Exception? exception = null) => Write("startup.log", message, exception);
    public void WriteApplication(string message, Exception? exception = null) => Write("application.log", message, exception);
    public void WriteCrash(string message, Exception? exception = null) => Write("crash.log", message, exception);
    public void WriteUpdate(string message, Exception? exception = null) => Write("update.log", message, exception);

    public void WriteStartupContext(bool safeMode, IReadOnlyCollection<string> arguments) => WriteStartup(
        $"Inicialização solicitada. safeMode={safeMode}; argumentos=[{string.Join(", ", arguments)}]; " +
        $"versão={AppMetadata.Version}; arquitetura={AppMetadata.Architecture}; OS={Environment.OSVersion}; " +
        $"diretório={AppContext.BaseDirectory}; processo={Environment.ProcessPath ?? "desconhecido"}; " +
        $"horário-local={DateTimeOffset.Now:O}");

    public string GetPath(string fileName) => Path.Combine(_rootDirectory, fileName);

    private void Write(string fileName, string message, Exception? exception)
    {
        var line = new StringBuilder()
            .Append('[').Append(DateTimeOffset.Now.ToString("O")).Append("] ")
            .Append(message);
        if (exception is not null) line.AppendLine().Append(exception);
        line.AppendLine();

        lock (_gate)
        {
            try
            {
                Directory.CreateDirectory(_rootDirectory);
                var path = GetPath(fileName);
                RotateIfNeeded(path);
                File.AppendAllText(path, line.ToString(), Encoding.UTF8);
            }
            catch
            {
                try
                {
                    var fallback = Path.Combine(Path.GetTempPath(), "PES2017EDITOR", "logs");
                    Directory.CreateDirectory(fallback);
                    File.AppendAllText(Path.Combine(fallback, fileName), line.ToString(), Encoding.UTF8);
                }
                catch { }
            }
        }
    }

    private static void RotateIfNeeded(string path)
    {
        if (!File.Exists(path) || new FileInfo(path).Length < MaxLogBytes) return;
        for (var index = 3; index >= 1; index--)
        {
            var source = index == 1 ? path : $"{path}.{index - 1}";
            var destination = $"{path}.{index}";
            if (File.Exists(source)) File.Move(source, destination, overwrite: true);
        }
    }
}
