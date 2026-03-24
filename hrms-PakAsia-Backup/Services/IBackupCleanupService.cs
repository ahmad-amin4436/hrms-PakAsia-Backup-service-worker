namespace hrms_PakAsia_Backup.Services
{
    public interface IBackupCleanupService
    {
        Task CleanupOldBackupsAsync(string backupPath, string prefix, int keepCount = 2);
    }
}
