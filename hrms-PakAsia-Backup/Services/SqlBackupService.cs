using Microsoft.Data.SqlClient;
using System.Text;

namespace hrms_PakAsia_Backup.Services
{
    public class SqlBackupService : ISqlBackupService
    {
        public async Task<string> BackupDatabaseAsync(SqlBackupConfig config)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupFileName = $"{config.DatabaseName}_backup_{timestamp}.bak";
            var fullBackupPath = Path.Combine(config.BackupPath, backupFileName);

            // Ensure backup directory exists
            Directory.CreateDirectory(config.BackupPath);

            var connectionString = BuildConnectionString(config);
            
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var backupCommand = $"BACKUP DATABASE [{config.DatabaseName}] TO DISK = '{fullBackupPath}' WITH FORMAT, INIT, NAME = '{config.DatabaseName}-Full Database Backup', SKIP, NOREWIND, NOUNLOAD, STATS = 10";

            using var command = new SqlCommand(backupCommand, connection);
            await command.ExecuteNonQueryAsync();

            return fullBackupPath;
        }

        private string BuildConnectionString(SqlBackupConfig config)
        {
            var sb = new SqlConnectionStringBuilder
            {
                DataSource = config.ServerName,
                InitialCatalog = "master",
                IntegratedSecurity = string.IsNullOrEmpty(config.Username) && string.IsNullOrEmpty(config.Password),
                TrustServerCertificate = true
            };

            if (!string.IsNullOrEmpty(config.Username))
            {
                sb.UserID = config.Username;
                sb.Password = config.Password;
                sb.IntegratedSecurity = false;
            }

            return sb.ConnectionString;
        }
    }
}
