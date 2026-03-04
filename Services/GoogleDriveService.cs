using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Drive.v3.Data;

namespace ChurchFacilityManagement.Services
{
    public class GoogleDriveService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleDriveService> _logger;
        private DriveService? _driveService;
        private const string ROOT_FOLDER_NAME = "MaintenanceImages";

        public GoogleDriveService(IConfiguration configuration, ILogger<GoogleDriveService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private async Task<DriveService> GetDriveServiceAsync()
        {
            if (_driveService != null)
                return _driveService;

            string jsonString;
            
            var credentialsJson = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS_JSON");
            
            if (!string.IsNullOrEmpty(credentialsJson))
            {
                jsonString = credentialsJson;
                _logger.LogInformation("Using credentials from environment variable");
            }
            else
            {
                var credentialsPath = _configuration["GoogleSheets:CredentialsPath"] ?? "credentials.json";
                jsonString = await System.IO.File.ReadAllTextAsync(credentialsPath);
                _logger.LogInformation("Using credentials from file");
            }

#pragma warning disable CS0618
            var credential = GoogleCredential.FromJson(jsonString)
                .CreateScoped(DriveService.Scope.Drive);
#pragma warning restore CS0618

            _driveService = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Church Facility Management"
            });

            return _driveService;
        }

        private async Task<string> GetOrCreateRootFolderAsync()
        {
            var driveService = await GetDriveServiceAsync();

            var request = driveService.Files.List();
            request.Q = $"name='{ROOT_FOLDER_NAME}' and mimeType='application/vnd.google-apps.folder' and trashed=false";
            request.Fields = "files(id, name)";
            
            var result = await request.ExecuteAsync();
            
            if (result.Files.Count > 0)
            {
                return result.Files[0].Id;
            }

            var folderMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = ROOT_FOLDER_NAME,
                MimeType = "application/vnd.google-apps.folder"
            };

            var createRequest = driveService.Files.Create(folderMetadata);
            createRequest.Fields = "id";
            var folder = await createRequest.ExecuteAsync();

            _logger.LogInformation($"Created root folder: {ROOT_FOLDER_NAME} with ID: {folder.Id}");
            return folder.Id;
        }

        private async Task<string> GetOrCreateRequestFolderAsync(int requestId)
        {
            var driveService = await GetDriveServiceAsync();
            var rootFolderId = await GetOrCreateRootFolderAsync();
            var folderName = requestId.ToString();

            var request = driveService.Files.List();
            request.Q = $"name='{folderName}' and '{rootFolderId}' in parents and mimeType='application/vnd.google-apps.folder' and trashed=false";
            request.Fields = "files(id, name)";
            
            var result = await request.ExecuteAsync();
            
            if (result.Files.Count > 0)
            {
                return result.Files[0].Id;
            }

            var folderMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder",
                Parents = new List<string> { rootFolderId }
            };

            var createRequest = driveService.Files.Create(folderMetadata);
            createRequest.Fields = "id";
            var folder = await createRequest.ExecuteAsync();

            _logger.LogInformation($"Created request folder: {folderName} with ID: {folder.Id}");
            return folder.Id;
        }

        public async Task<string> UploadImageAsync(int requestId, IFormFile file)
        {
            var driveService = await GetDriveServiceAsync();
            var folderId = await GetOrCreateRequestFolderAsync(requestId);

            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = file.FileName,
                Parents = new List<string> { folderId }
            };

            FilesResource.CreateMediaUpload uploadRequest;
            using (var stream = file.OpenReadStream())
            {
                uploadRequest = driveService.Files.Create(fileMetadata, stream, file.ContentType);
                uploadRequest.Fields = "id, webViewLink, webContentLink";
                await uploadRequest.UploadAsync();
            }

            var uploadedFile = uploadRequest.ResponseBody;

            var permission = new Permission()
            {
                Type = "anyone",
                Role = "reader"
            };
            await driveService.Permissions.Create(permission, uploadedFile.Id).ExecuteAsync();

            _logger.LogInformation($"Uploaded file {file.FileName} to request {requestId}. File ID: {uploadedFile.Id}");
            return uploadedFile.WebViewLink;
        }

        public async Task<List<string>> UploadMultipleImagesAsync(int requestId, List<IFormFile> files)
        {
            var links = new List<string>();
            
            foreach (var file in files)
            {
                if (file.Length > 5 * 1024 * 1024)
                {
                    _logger.LogWarning($"File {file.FileName} exceeds 5MB limit and will be skipped");
                    continue;
                }

                var link = await UploadImageAsync(requestId, file);
                links.Add(link);
            }

            return links;
        }
    }
}
