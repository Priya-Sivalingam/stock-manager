using Microsoft.EntityFrameworkCore;

namespace StockManager.Data;

public static class BackupService
{
    public static string BackupFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "StockManagerBackups");

    // Returns the backup file path, or null if today's backup already exists and onlyIfNoneToday is true
    public static async Task<string?> BackupAsync(bool onlyIfNoneToday)
    {
        Directory.CreateDirectory(BackupFolder);
        var file = Path.Combine(BackupFolder, $"stock-{DateTime.Now:yyyyMMdd}.db");

        if (File.Exists(file))
        {
            if (onlyIfNoneToday) return null;
            File.Delete(file);
        }

        using var db = new AppDbContext();
        await db.Database.ExecuteSqlRawAsync($"VACUUM INTO '{file.Replace("'", "''")}'");

        // keep the latest 30 backups
        foreach (var old in Directory.GetFiles(BackupFolder, "stock-*.db")
                                     .OrderByDescending(f => f).Skip(30))
            File.Delete(old);

        return file;
    }
}