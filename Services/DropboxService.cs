using Dropbox.Api;
using Dropbox.Api.Files;
using Dropbox.Api.Sharing;

namespace ChurchFacilityManagement.Services
{
    public class DropboxService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DropboxService> _logger;
        private DropboxClient? _dropboxClient;
        private const string ROOT_FOLDER = "/MaintenanceImages";

        public DropboxService(IConfiguration configuration, ILogger<DropboxService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private DropboxClient GetDropboxClient()
        {
            if (_dropboxClient != null)
                return _dropboxClient;

            // Check environment variable first (for Cloud Run deployment)
            var accessToken = Environment.GetEnvironmentVariable("DROPBOX_ACCESS_TOKEN");

            if (!string.IsNullOrEmpty(accessToken))
            {
                _logger.LogInformation("Using Dropbox access token from environment variable");
            }
            else
            {
                // Fall back to appsettings.json (for local development)
                accessToken = _configuration["Dropbox:AccessToken"];
                _logger.LogInformation("Using Dropbox access token from appsettings.json");
            }

            if (string.IsNullOrEmpty(accessToken))
            {
                var errorMsg = "Dropbox access token not found in environment variable or appsettings.json";
                _logger.LogError(errorMsg);
                throw new Exception(errorMsg);
            }

            _dropboxClient = new DropboxClient(accessToken);
            _logger.LogInformation("Dropbox client initialized successfully");

            return _dropboxClient;
        }

        public async Task<string> UploadImageAsync(int requestId, IFormFile file)
        {
            var client = GetDropboxClient();

            // Create filename with request ID prefix
            var fileName = $"{requestId}_{file.FileName}";
            var dropboxPath = $"{ROOT_FOLDER}/{fileName}";

            try
            {
                _logger.LogInformation($"Uploading file {fileName} to Dropbox at {dropboxPath}");

                // Upload the file
                using (var stream = file.OpenReadStream())
                {
                    var uploadResult = await client.Files.UploadAsync(
                        dropboxPath,
                        WriteMode.Overwrite.Instance,
                        body: stream
                    );

                    _logger.LogInformation($"File uploaded successfully. Dropbox ID: {uploadResult.Id}");
                }

                // Create a shared link for the file
                try
                {
                    var sharedLinkResult = await client.Sharing.CreateSharedLinkWithSettingsAsync(dropboxPath);

                    var shareableLink = sharedLinkResult.Url;
                    _logger.LogInformation($"Created shareable link: {shareableLink}");
                    return shareableLink;
                }
                catch (Exception ex)
                {
                    // If link already exists, get existing link
                    _logger.LogWarning($"Could not create new shared link, attempting to get existing: {ex.Message}");

                    var existingLinks = await client.Sharing.ListSharedLinksAsync(dropboxPath);
                    if (existingLinks.Links.Count > 0)
                    {
                        var link = existingLinks.Links[0].Url;
                        _logger.LogInformation($"Using existing shared link: {link}");
                        return link;
                    }

                    throw new Exception($"Could not create or retrieve shared link: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading file {fileName} to Dropbox");
                throw new Exception($"Failed to upload file to Dropbox: {ex.Message}", ex);
            }
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

                try
                {
                    var link = await UploadImageAsync(requestId, file);
                    links.Add(link);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to upload {file.FileName}, skipping");
                    // Continue with other files even if one fails
                }
            }

            return links;
        }
    }
}
