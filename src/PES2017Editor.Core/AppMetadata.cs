using System.Reflection;

namespace PES2017Editor.Core;

public static class AppMetadata
{
    public const string ProductName = "PES 2017 Database Editor";
    public const string RepositoryUrl = "https://github.com/xXxjubeixXx/PES2017EDITOR";
    public const string ReleasesUrl = RepositoryUrl + "/releases";
    public const string IssuesUrl = RepositoryUrl + "/issues";

    public static string Version
    {
        get
        {
            var assembly = Assembly.GetEntryAssembly() ?? typeof(AppMetadata).Assembly;
            return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? assembly.GetName().Version?.ToString(3)
                ?? "0.0.0";
        }
    }

    public static string BuildDate
    {
        get
        {
            var assembly = Assembly.GetEntryAssembly() ?? typeof(AppMetadata).Assembly;
            return assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(attribute => attribute.Key.Equals("BuildDate", StringComparison.OrdinalIgnoreCase))?.Value
                ?? "not specified";
        }
    }

    public static string Architecture => Environment.Is64BitProcess ? "Windows x64" : "Windows x86";
}
