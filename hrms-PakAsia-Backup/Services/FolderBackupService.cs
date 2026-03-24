using System.IO.Compression;

namespace hrms_PakAsia_Backup.Services
{
    public class FolderBackupService : IFolderBackupService
    {
        public async Task<string> BackupFolderAsync(FolderBackupConfig config)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var zipFileName = $"{config.FolderName}_backup_{timestamp}.zip";
            var fullBackupPath = Path.Combine(config.BackupPath, zipFileName);

            // Ensure backup directory exists
            Directory.CreateDirectory(config.BackupPath);

            // Check if source folder exists
            if (!Directory.Exists(config.SourcePath))
            {
                throw new DirectoryNotFoundException($"Source folder not found: {config.SourcePath}");
            }

            // Create zip file
            using (var archive = ZipFile.Open(fullBackupPath, ZipArchiveMode.Create))
            {
                var sourceFolder = new DirectoryInfo(config.SourcePath);
                await AddFilesToArchiveAsync(archive, sourceFolder, config);
            }

            return fullBackupPath;
        }

        private async Task AddFilesToArchiveAsync(ZipArchive archive, DirectoryInfo directory, FolderBackupConfig config)
        {
            foreach (var file in directory.GetFiles())
            {
                var relativePath = Path.GetRelativePath(config.SourcePath, file.FullName);
                var entryPath = Path.Combine(config.FolderName, relativePath);
                var entry = archive.CreateEntry(entryPath.Replace('\\', '/'));
                
                using (var entryStream = entry.Open())
                using (var fileStream = file.OpenRead())
                {
                    await fileStream.CopyToAsync(entryStream);
                }
            }

            foreach (var subDirectory in directory.GetDirectories())
            {
                await AddFilesToArchiveAsync(archive, subDirectory, config);
            }
        }
    }
}
