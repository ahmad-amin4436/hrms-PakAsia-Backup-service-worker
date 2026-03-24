# HRMS Backup Service

This is a .NET 8.0 Worker Service that automatically backs up SQL Server databases and IIS published folders, then uploads the backups to Google Drive every 30 minutes.

## Features

- **SQL Server Database Backup**: Supports multiple databases with configurable connection settings
- **IIS Folder Backup**: Compresses published web application folders into ZIP files
- **Google Drive Integration**: Uploads backup files to a specified Google Drive folder
- **Automated Scheduling**: Runs every 30 minutes (configurable)
- **Automatic Cleanup**: Keeps only the last 2 backups for each database and folder
- **Error Handling**: Comprehensive logging and error recovery
- **Configurable**: All settings managed through appsettings.json

## Prerequisites

1. .NET 8.0 Runtime
2. SQL Server access with backup permissions
3. Google Drive API credentials
4. Sufficient disk space for temporary backup files

## Setup Instructions

### 1. Google Drive API Setup

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select existing one
3. Enable Google Drive API
4. Create OAuth 2.0 credentials (Desktop Application)
5. Download the credentials JSON file and rename it to `credentials.json`
6. Place `credentials.json` in the application directory

### 2. Configuration

Edit `appsettings.json` to configure your backup settings:

```json
{
  "BackupSettings": {
    "IntervalMinutes": 30,
    "SqlBackups": [
      {
        "ServerName": "localhost",
        "DatabaseName": "HRMS_Database",
        "BackupPath": "C:\\Backups\\SQL",
        "Username": "",
        "Password": ""
      }
    ],
    "FolderBackups": [
      {
        "SourcePath": "C:\\inetpub\\wwwroot\\HRMS",
        "BackupPath": "C:\\Backups\\Folders",
        "FolderName": "HRMS_WebApp"
      }
    ],
    "GoogleDrive": {
      "FolderId": "your-google-drive-folder-id",
      "CredentialsPath": "credentials.json",
      "TokenPath": "token.json"
    }
  }
}
```

### 3. Get Google Drive Folder ID

1. Open Google Drive and create a folder for backups
2. Navigate to the folder
3. The folder ID is in the URL: `https://drive.google.com/drive/folders/FOLDER_ID`

### 4. Run the Service

#### Development:
```bash
dotnet run
```

#### Production (Windows Service):
```bash
# Install as Windows Service
sc create "HRMS Backup Service" binPath="path\to\hrms-PakAsia-Backup.exe"
sc start "HRMS Backup Service"
```

## Configuration Details

### SQL Backup Configuration
- **ServerName**: SQL Server instance name
- **DatabaseName**: Database to backup
- **BackupPath**: Local path for temporary backup files
- **Username/Password**: SQL authentication (leave empty for Windows auth)

### Folder Backup Configuration
- **SourcePath**: Path to IIS published folder
- **BackupPath**: Local path for temporary ZIP files
- **FolderName**: Name used in ZIP filename

### Google Drive Configuration
- **FolderId**: Target Google Drive folder ID
- **CredentialsPath**: Path to credentials JSON file
- **TokenPath**: Path for OAuth token storage

## File Naming Convention

- SQL Backups: `{DatabaseName}_backup_{yyyyMMdd_HHmmss}.bak`
- Folder Backups: `{FolderName}_backup_{yyyyMMdd_HHmmss}.zip`

## Automatic Cleanup

The service automatically manages disk space by keeping only the last 2 backup files for each:
- Database: `PakAsia_HRMS_backup_*.bak` (keeps latest 2)
- Database: `PharmacyDB_backup_*.bak` (keeps latest 2)  
- Folder: `HRMS_WebApp_backup_*.zip` (keeps latest 2)

Older backup files are automatically deleted after each successful backup cycle to prevent disk space issues.

## Logging

The service logs all operations including:
- Backup start/completion times
- Success/failure status for each operation
- Error details with stack traces
- Next scheduled backup time

Logs are output to the console and can be configured for file logging.

## Security Considerations

1. Store credentials.json securely
2. Use SQL authentication with limited permissions
3. Ensure backup directories have appropriate access controls
4. Consider encrypting sensitive configuration values

## Troubleshooting

### Common Issues

1. **SQL Backup Fails**: Check SQL Server permissions and connectivity
2. **Google Drive Upload Fails**: Verify credentials.json and folder ID
3. **Folder Backup Fails**: Ensure source path exists and is accessible
4. **Service Won't Start**: Check .NET 8.0 runtime installation

### First Run Authentication

The first time the service runs, it will open a browser window for Google Drive authentication. Complete the authorization flow to generate the token.json file.

## Maintenance

- Monitor log files for errors
- Clean up old backup files periodically
- Update Google Drive credentials if they expire
- Review backup retention policies

## License

This project is proprietary software for HRMS backup operations.
