namespace hrms_PakAsia_Backup.Services
{
    public class BackupCleanupService : IBackupCleanupService
    {
        public async Task CleanupOldBackupsAsync(string backupPath, string prefix, int keepCount = 2)
        {
            await Task.Run(() =>
            {
                if (!Directory.Exists(backupPath))
                    return;

                var files = Directory.GetFiles(backupPath, $"{prefix}*")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                if (files.Count <= keepCount)
                    return;

                var filesToDelete = files.Skip(keepCount);
                
                foreach (var file in filesToDelete)
                {
                    try
                    {
                        file.Delete();
                        Console.WriteLine($"Deleted old backup: {file.Name}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to delete old backup {file.Name}: {ex.Message}");
                    }
                }
            });
        }
    }
}
