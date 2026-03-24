namespace hrms_PakAsia_Backup.Services
{
    public interface IGoogleDriveCleanupService
    {
        Task CleanupOldFilesAsync(string folderId, string fileNamePrefix, int keepCount = 1);
    }
}
