using Dropbox.Api;
using Dropbox.Api.Files;
using Dropbox.Api.Sharing;

namespace ChurchFacilityManagement.Services
{
    public class DropboxService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DropboxService> _logger;
        private readonly DropboxOAuthService _oauthService;
        private DropboxClient? _dropboxClient;
        private const string ROOT_FOLDER = "/MaintenanceImages";

        public DropboxService(
            IConfiguration configuration, 
            ILogger<DropboxService> logger,
            DropboxOAuthService oauthService)
        {
            _configuration = configuration;
            _logger = logger;
            _oauthService = oauthService;
        }

        private async Task<DropboxClient> GetDropboxClientAsync()
        {
            // Try to get valid tokens via OAuth (refresh token flow)
            var tokens = await _oauthService.GetValidTokensAsync();

            if (tokens != null && !string.IsNullOrEmpty(tokens.AccessToken))
            {
                _logger.LogInformation("Using Dropbox access token from OAuth refresh token");
                _dropboxClient = new DropboxClient(tokens.AccessToken);
                return _dropboxClient;
            }

            // Fallback: Check environment variable for direct access token (backward compatibility)
            var accessToken = Environment.GetEnvironmentVariable("DROPBOX_ACCESS_TOKEN");

            if (!string.IsNullOrEmpty(accessToken))
            {
                _logger.LogInformation("Using Dropbox access token from environment variable (fallback)");
                _dropboxClient = new DropboxClient(accessToken);
                return _dropboxClient;
            }

            // Last fallback: appsettings.json (for legacy local development)
            accessToken = _configuration["Dropbox:AccessToken"];
            if (!string.IsNullOrEmpty(accessToken))
            {
                _logger.LogInformation("Using Dropbox access token from appsettings.json (fallback)");
                _dropboxClient = new DropboxClient(accessToken);
                return _dropboxClient;
            }

            var errorMsg = "Dropbox not configured. Please run OAuth setup or set DROPBOX_REFRESH_TOKEN";
            _logger.LogError(errorMsg);
            throw new Exception(errorMsg);
        }

        public async Task<string> UploadImageAsync(int requestId, IFormFile file)
        {
            var client = await GetDropboxClientAsync();

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
