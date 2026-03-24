using Google.Apis.Drive.v3;

namespace hrms_PakAsia_Backup.Services
{
    public interface IGoogleDriveService
    {
        Task UploadFileAsync(string filePath, string folderId);
        DriveService GetDriveService();
    }
}
