using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;

namespace hrms_PakAsia_Backup.Services
{
    public class GoogleDriveCleanupService : IGoogleDriveCleanupService
    {
        private readonly DriveService _driveService;

        public GoogleDriveCleanupService(DriveService driveService)
        {
            _driveService = driveService;
        }

        public async Task CleanupOldFilesAsync(string folderId, string fileNamePrefix, int keepCount = 1)
        {
            try
            {
                // Get all files with the specified prefix
                var listRequest = _driveService.Files.List();
                listRequest.Q = $"name contains '{fileNamePrefix}' and trashed=false";
                listRequest.Fields = "files(id, name, createdTime)";
                listRequest.OrderBy = "createdTime desc";

                var result = await listRequest.ExecuteAsync();
                var files = result.Files.ToList();

                if (files.Count <= keepCount)
                    return;

                // Get files to delete (skip the first 'keepCount' files)
                var filesToDelete = files.Skip(keepCount);

                foreach (var file in filesToDelete)
                {
                    try
                    {
                        var deleteRequest = _driveService.Files.Delete(file.Id);
                        await deleteRequest.ExecuteAsync();
                        Console.WriteLine($"Deleted old Google Drive file: {file.Name}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to delete Google Drive file {file.Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to cleanup Google Drive files: {ex.Message}");
            }
        }
    }
}
