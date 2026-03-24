namespace hrms_PakAsia_Backup.Services
{
    public interface ISqlBackupService
    {
        Task<string> BackupDatabaseAsync(SqlBackupConfig config);
    }
}
