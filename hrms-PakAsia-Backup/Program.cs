using hrms_PakAsia_Backup;
using hrms_PakAsia_Backup.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService();
// Configure appsettings.json
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Bind configuration
var backupSettings = new BackupSettings();
builder.Configuration.GetSection("BackupSettings").Bind(backupSettings);

// Register services
builder.Services.AddSingleton(backupSettings);
builder.Services.AddSingleton<ISqlBackupService, SqlBackupService>();
builder.Services.AddSingleton<IFolderBackupService, FolderBackupService>();
builder.Services.AddSingleton<IGoogleDriveService>(provider => 
    new GoogleDriveService(provider.GetRequiredService<BackupSettings>().GoogleDrive));
builder.Services.AddSingleton<IBackupCleanupService, BackupCleanupService>();
builder.Services.AddSingleton<IGoogleDriveCleanupService>(provider => 
    new GoogleDriveCleanupService(provider.GetRequiredService<IGoogleDriveService>().GetDriveService()));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
