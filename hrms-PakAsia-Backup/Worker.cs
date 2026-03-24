using hrms_PakAsia_Backup.Services;

namespace hrms_PakAsia_Backup
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly BackupSettings _settings;
        private readonly ISqlBackupService _sqlBackupService;
        private readonly IFolderBackupService _folderBackupService;
        private readonly IGoogleDriveService _googleDriveService;
        private readonly IBackupCleanupService _backupCleanupService;
        private readonly IGoogleDriveCleanupService _googleDriveCleanupService;

        public Worker(
            ILogger<Worker> logger,
            BackupSettings settings,
            ISqlBackupService sqlBackupService,
            IFolderBackupService folderBackupService,
            IGoogleDriveService googleDriveService,
            IBackupCleanupService backupCleanupService,
            IGoogleDriveCleanupService googleDriveCleanupService)
        {
            _logger = logger;
            _settings = settings;
            _sqlBackupService = sqlBackupService;
            _folderBackupService = folderBackupService;
            _googleDriveService = googleDriveService;
            _backupCleanupService = backupCleanupService;
            _googleDriveCleanupService = googleDriveCleanupService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("HRMS Backup Service started");
            _logger.LogInformation("Backup interval: {interval} minutes", _settings.IntervalMinutes);

            // Run immediately on startup
            await PerformBackupAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = TimeSpan.FromMinutes(_settings.IntervalMinutes);
                _logger.LogInformation("Next backup scheduled at: {time}", DateTimeOffset.Now.Add(delay));

                await Task.Delay(delay, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    await PerformBackupAsync();
                }
            }
        }

        private async Task PerformBackupAsync()
        {
            _logger.LogInformation("Starting backup process at: {time}", DateTimeOffset.Now);
            var backupFiles = new List<string>();

            try
            {
                // Backup SQL databases
                foreach (var sqlConfig in _settings.SqlBackups)
                {
                    try
                    {
                        _logger.LogInformation("Backing up SQL database: {database}", sqlConfig.DatabaseName);
                        var backupFile = await _sqlBackupService.BackupDatabaseAsync(sqlConfig);
                        backupFiles.Add(backupFile);
                        _logger.LogInformation("SQL database backup completed: {file}", backupFile);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to backup SQL database: {database}", sqlConfig.DatabaseName);
                    }
                }

                // Backup folders
                foreach (var folderConfig in _settings.FolderBackups)
                {
                    try
                    {
                        _logger.LogInformation("Backing up folder: {folder}", folderConfig.SourcePath);
                        var backupFile = await _folderBackupService.BackupFolderAsync(folderConfig);
                        backupFiles.Add(backupFile);
                        _logger.LogInformation("Folder backup completed: {file}", backupFile);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to backup folder: {folder}", folderConfig.SourcePath);
                    }
                }

                // Upload to Google Drive
                foreach (var file in backupFiles)
                {
                    try
                    {
                        _logger.LogInformation("Uploading to Google Drive: {file}", file);
                        await _googleDriveService.UploadFileAsync(file, _settings.GoogleDrive.FolderId);
                        _logger.LogInformation("Upload completed: {file}", file);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to upload to Google Drive: {file}", file);
                    }
                }

                // Cleanup local backup files (optional - you might want to keep them)
                // await CleanupLocalFilesAsync(backupFiles);

                // Cleanup old backups - keep only last 2
                await CleanupOldBackupsAsync();

                _logger.LogInformation("Backup process completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backup process failed");
            }
        }

        private async Task CleanupOldBackupsAsync()
        {
            try
            {
                // Cleanup SQL database backups
                foreach (var sqlConfig in _settings.SqlBackups)
                {
                    // Cleanup local files
                    await _backupCleanupService.CleanupOldBackupsAsync(
                        sqlConfig.BackupPath, 
                        $"{sqlConfig.DatabaseName}_backup_", 
                        1);

                    // Cleanup Google Drive files
                    await _googleDriveCleanupService.CleanupOldFilesAsync(
                        _settings.GoogleDrive.FolderId,
                        $"{sqlConfig.DatabaseName}_backup_",
                        1);
                }

                // Cleanup folder backups
                foreach (var folderConfig in _settings.FolderBackups)
                {
                    // Cleanup local files
                    await _backupCleanupService.CleanupOldBackupsAsync(
                        folderConfig.BackupPath, 
                        $"{folderConfig.FolderName}_backup_", 
                        1);

                    // Cleanup Google Drive files
                    await _googleDriveCleanupService.CleanupOldFilesAsync(
                        _settings.GoogleDrive.FolderId,
                        $"{folderConfig.FolderName}_backup_",
                        1);
                }

                _logger.LogInformation("Old backup cleanup completed (local and Google Drive)");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup old backups");
            }
        }

        private Task CleanupLocalFilesAsync(List<string> files)
        {
            return Task.Run(() =>
            {
                foreach (var file in files)
                {
                    try
                    {
                        if (File.Exists(file))
                        {
                            File.Delete(file);
                            _logger.LogInformation("Deleted local backup file: {file}", file);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete local backup file: {file}", file);
                    }
                }
            });
        }
    }
}
