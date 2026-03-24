using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using Google.Apis.Util;

namespace hrms_PakAsia_Backup.Services
{
    public class GoogleDriveService : IGoogleDriveService
    {
        private readonly DriveService _driveService;
        private readonly GoogleDriveConfig _config;
        private UserCredential _credential;

        public GoogleDriveService(GoogleDriveConfig config)
        {
            _config = config;
            _driveService = CreateDriveService(config);
        }

        public DriveService GetDriveService() => _driveService;

        public async Task UploadFileAsync(string filePath, string folderId)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            // Check if token needs refresh before uploading
            await EnsureValidCredentialAsync();

            var fileName = Path.GetFileName(filePath);
            var fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = fileName
            };

            // Only add parent if folderId is not the placeholder
            if (!string.IsNullOrEmpty(folderId) && folderId != "your-google-drive-folder-id")
            {
                fileMetadata.Parents = new List<string> { folderId };
            }

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var request = _driveService.Files.Create(fileMetadata, stream, GetMimeType(filePath));
            request.Fields = "id, name, size";

            var progress = await request.UploadAsync();
            
            if (progress.Status != UploadStatus.Completed)
            {
                throw new Exception($"Upload failed: {progress.Exception?.Message}");
            }

            var uploadedFile = request.ResponseBody;
            Console.WriteLine($"File '{uploadedFile.Name}' uploaded to Google Drive with ID: {uploadedFile.Id}");
        }

        private async Task EnsureValidCredentialAsync()
        {
            if (_credential == null || _credential.Token.IsExpired(SystemClock.Default))
            {
                await RefreshCredentialAsync();
            }
        }

        private async Task RefreshCredentialAsync()
        {
            try
            {
                if (_credential != null && await _credential.RefreshTokenAsync(CancellationToken.None))
                {
                    Console.WriteLine("Google Drive token refreshed successfully");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to refresh token: {ex.Message}");
            }

            // If refresh fails, try to get existing token without prompting
            Console.WriteLine("Attempting to load existing Google Drive credential...");
            _credential = await TryLoadExistingCredentialAsync(_config);
            
            if (_credential == null)
            {
                throw new Exception(
                    "Google Drive authentication required. Please run the application once in console mode to authenticate, " +
                    "or copy a valid token.json file to the application directory. " +
                    "Windows Services cannot perform interactive browser authentication.");
            }
        }

        private async Task<UserCredential> TryLoadExistingCredentialAsync(GoogleDriveConfig config)
        {
            try
            {
                var credentialsPath = GetFullPath(config.CredentialsPath);
                using var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read);
                var secrets = GoogleClientSecrets.FromStream(stream).Secrets;
                
                // Try to load from token store without prompting
                var tokenPath = GetFullPath(config.TokenPath);
                var dataStore = new FileDataStore(tokenPath, true);
                var token = await dataStore.GetAsync<TokenResponse>("user");
                
                if (token != null && !string.IsNullOrEmpty(token.RefreshToken))
                {
                    return new UserCredential(new Google.Apis.Auth.OAuth2.Flows.GoogleAuthorizationCodeFlow(
                        new Google.Apis.Auth.OAuth2.Flows.GoogleAuthorizationCodeFlow.Initializer
                        {
                            ClientSecrets = secrets,
                            DataStore = dataStore
                        }), "user", token);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load existing credential: {ex.Message}");
            }
            
            return null;
        }

        private string GetFullPath(string relativePath)
        {
            if (Path.IsPathRooted(relativePath))
            {
                return relativePath;
            }
            
            // Get the application's base directory (works for both console and service)
            var appDirectory = AppContext.BaseDirectory;
            return Path.Combine(appDirectory, relativePath);
        }

        private DriveService CreateDriveService(GoogleDriveConfig config)
        {
            _credential = GetCredential(config);
            
            return new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = _credential,
                ApplicationName = "HRMS Backup Service"
            });
        }

        private UserCredential GetCredential(GoogleDriveConfig config)
        {
            try
            {
                // First try to load existing credential without prompting
                var existingCredential = TryLoadExistingCredentialAsync(config).Result;
                if (existingCredential != null)
                {
                    Console.WriteLine("Using existing Google Drive credential");
                    return existingCredential;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not load existing credential: {ex.Message}");
            }

            // If no existing credential, try interactive authentication
            try
            {
                Console.WriteLine("Attempting interactive Google Drive authentication...");
                var credentialsPath = GetFullPath(config.CredentialsPath);
                using var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read);
                
                var tokenPath = GetFullPath(config.TokenPath);
                return GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    new[] { DriveService.Scope.DriveFile },
                    "user",
                    CancellationToken.None,
                    new FileDataStore(tokenPath, true)).Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Interactive authentication failed: {ex.Message}");
                throw new Exception(
                    "Google Drive authentication failed. For Windows Service deployment, please:\n" +
                    "1. Run the application once in console mode to complete authentication\n" +
                    "2. Copy the resulting token.json file to the service directory\n" +
                    "3. Restart the service", ex);
            }
        }

        private string GetMimeType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".bak" => "application/octet-stream",
                ".zip" => "application/zip",
                _ => "application/octet-stream"
            };
        }
    }
}
