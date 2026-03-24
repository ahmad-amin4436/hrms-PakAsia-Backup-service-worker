namespace hrms_PakAsia_Backup.Services
{
    public interface IFolderBackupService
    {
        Task<string> BackupFolderAsync(FolderBackupConfig config);
    }
}
