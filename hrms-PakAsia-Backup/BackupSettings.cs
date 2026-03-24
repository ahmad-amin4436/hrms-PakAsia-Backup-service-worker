namespace hrms_PakAsia_Backup
{
    public class BackupSettings
    {
        public int IntervalMinutes { get; set; } = 30;
        public List<SqlBackupConfig> SqlBackups { get; set; } = new();
        public List<FolderBackupConfig> FolderBackups { get; set; } = new();
        public GoogleDriveConfig GoogleDrive { get; set; } = new();
    }

    public class SqlBackupConfig
    {
        public string ServerName { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string BackupPath { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class FolderBackupConfig
    {
        public string SourcePath { get; set; } = string.Empty;
        public string BackupPath { get; set; } = string.Empty;
        public string FolderName { get; set; } = string.Empty;
    }

    public class GoogleDriveConfig
    {
        public string FolderId { get; set; } = string.Empty;
        public string CredentialsPath { get; set; } = string.Empty;
        public string TokenPath { get; set; } = string.Empty;
    }
}
