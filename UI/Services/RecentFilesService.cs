using System.IO;
using UI.Properties;

namespace UI.Services;

/// <summary>
/// Manages the recently opened files list, persisted via application settings.
/// </summary>
public sealed class RecentFilesService
{
    private const int MaxRecentFiles = 10;

    /// <summary>
    /// Returns the current list of recently opened file paths,
    /// most recent first.
    /// </summary>
    public IReadOnlyList<string> GetRecentFiles()
    {
        var files = Settings.Default.RecentFiles;
        if (files is null) return [];

        return files.Cast<string>()
                    .Where(File.Exists)
                    .ToList();
    }

    /// <summary>
    /// Adds a file path to the top of the recent files list.
    /// Removes duplicates and trims to <see cref="MaxRecentFiles"/>.
    /// </summary>
    public void AddRecentFile(string path)
    {
        var files = Settings.Default.RecentFiles ?? [];

        // Remove if already present — will be re-added at the top
        files.Remove(path);

        // Insert at the beginning
        files.Insert(0, path);

        // Trim to max
        while (files.Count > MaxRecentFiles)
            files.RemoveAt(files.Count - 1);

        Settings.Default.RecentFiles = files;
        Settings.Default.Save();
    }

    /// <summary>
    /// Removes a file path from the recent files list.
    /// </summary>
    public void RemoveRecentFile(string path)
    {
        var files = Settings.Default.RecentFiles;
        if (files is null) return;

        files.Remove(path);
        Settings.Default.RecentFiles = files;
        Settings.Default.Save();
    }

    /// <summary>Clears all recent files.</summary>
    public void ClearRecentFiles()
    {
        Settings.Default.RecentFiles = [];
        Settings.Default.Save();
    }
}