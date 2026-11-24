namespace PES2017Editor.Infrastructure;

public static class AppDataPaths
{
    public static string Root => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PES2017EDITOR");

    public static string CacheDirectory => Path.Combine(Root, "cache");
    public static string UpdatesDirectory => Path.Combine(Root, "updates");
    public static string LogsDirectory => Path.Combine(Root, "logs");
    public static string ProjectsDirectory => Path.Combine(Root, "projects");
    public static string BackupsDirectory => Path.Combine(Root, "backups");
    public static string SettingsPath => Path.Combine(Root, "settings.json");
    public static string RecentInstallationsPath => Path.Combine(Root, "recent-installations.json");
    public static string UpdateHistoryPath => Path.Combine(Root, "update-history.json");
}
