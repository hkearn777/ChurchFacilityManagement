using Dropbox.Api;
using System.Text.Json;

namespace ChurchFacilityManagement.Services
{
    public class DropboxOAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DropboxOAuthService> _logger;
        private const string TOKEN_FILE = "dropbox_tokens.json";

        public DropboxOAuthService(IConfiguration configuration, ILogger<DropboxOAuthService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public class DropboxTokens
        {
            public string? AccessToken { get; set; }
            public string? RefreshToken { get; set; }
            public DateTime ExpiresAt { get; set; }
        }

        public string GetAuthorizationUrl()
        {
            var appKey = GetAppKey();
            var redirectUri = GetRedirectUri();

            var authorizeUri = DropboxOAuth2Helper.GetAuthorizeUri(
                OAuthResponseType.Code,
                appKey,
                redirectUri,
                tokenAccessType: TokenAccessType.Offline // This requests a refresh token
            );

            return authorizeUri.ToString();
        }

        public async Task<DropboxTokens> ExchangeCodeForTokenAsync(string code)
        {
            var appKey = GetAppKey();
            var appSecret = GetAppSecret();
            var redirectUri = GetRedirectUri();

            try
            {
                _logger.LogInformation("Exchanging authorization code for tokens");

                var response = await DropboxOAuth2Helper.ProcessCodeFlowAsync(
                    code,
                    appKey,
                    appSecret,
                    redirectUri
                );

                var tokens = new DropboxTokens
                {
                    AccessToken = response.AccessToken,
                    RefreshToken = response.RefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddSeconds(14400) // Default 4 hours
                };

                await SaveTokensAsync(tokens);
                _logger.LogInformation("Tokens obtained and saved successfully");

                return tokens;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exchanging code for tokens");
                throw new Exception($"Failed to obtain Dropbox tokens: {ex.Message}", ex);
            }
        }

        public async Task<DropboxTokens?> GetValidTokensAsync()
        {
            var tokens = await LoadTokensAsync();

            if (tokens == null || string.IsNullOrEmpty(tokens.RefreshToken))
            {
                _logger.LogWarning("No refresh token available");
                return null;
            }

            // If token is expired or will expire in the next 5 minutes, refresh it
            if (string.IsNullOrEmpty(tokens.AccessToken) || tokens.ExpiresAt <= DateTime.UtcNow.AddMinutes(5))
            {
                _logger.LogInformation("Access token expired or expiring soon, refreshing");
                tokens = await RefreshAccessTokenAsync(tokens.RefreshToken);
            }

            return tokens;
        }

        private async Task<DropboxTokens> RefreshAccessTokenAsync(string refreshToken)
        {
            var appKey = GetAppKey();
            var appSecret = GetAppSecret();

            try
            {
                _logger.LogInformation("Refreshing access token");

                // Use the DropboxClient with refresh token to get a new access token
                using var httpClient = new HttpClient();
                var requestBody = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", refreshToken),
                    new KeyValuePair<string, string>("client_id", appKey),
                    new KeyValuePair<string, string>("client_secret", appSecret)
                });

                var response = await httpClient.PostAsync("https://api.dropbox.com/oauth2/token", requestBody);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(responseContent);
                var root = jsonDoc.RootElement;

                var newAccessToken = root.GetProperty("access_token").GetString();

                var tokens = new DropboxTokens
                {
                    AccessToken = newAccessToken,
                    RefreshToken = refreshToken, // Refresh token stays the same
                    ExpiresAt = DateTime.UtcNow.AddSeconds(14400) // 4 hours
                };

                await SaveTokensAsync(tokens);
                _logger.LogInformation("Access token refreshed successfully");

                return tokens;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing access token");
                throw new Exception($"Failed to refresh Dropbox access token: {ex.Message}", ex);
            }
        }

        private async Task SaveTokensAsync(DropboxTokens tokens)
        {
            try
            {
                // For production (Cloud Run), store in environment or Secret Manager
                // For local development, store in a local file
                var isProduction = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("K_SERVICE"));

                if (isProduction)
                {
                    _logger.LogInformation("Production environment: Tokens should be stored in Secret Manager");
                    // In production, we rely on Secret Manager
                    // The refresh token should be stored there manually or via deployment script
                }
                else
                {
                    // Local development: save to file
                    var json = JsonSerializer.Serialize(tokens, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(TOKEN_FILE, json);
                    _logger.LogInformation($"Tokens saved to {TOKEN_FILE}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving tokens");
                throw;
            }
        }

        private async Task<DropboxTokens?> LoadTokensAsync()
        {
            try
            {
                // Check environment variable first (for production)
                var refreshToken = Environment.GetEnvironmentVariable("DROPBOX_REFRESH_TOKEN");
                
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    _logger.LogInformation("Using refresh token from environment variable");
                    return new DropboxTokens
                    {
                        RefreshToken = refreshToken,
                        AccessToken = null, // Will be refreshed
                        ExpiresAt = DateTime.MinValue
                    };
                }

                // For local development, load from file
                if (File.Exists(TOKEN_FILE))
                {
                    var json = await File.ReadAllTextAsync(TOKEN_FILE);
                    var tokens = JsonSerializer.Deserialize<DropboxTokens>(json);
                    _logger.LogInformation("Tokens loaded from local file");
                    return tokens;
                }

                _logger.LogWarning("No tokens found");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading tokens");
                return null;
            }
        }

        private string GetAppKey()
        {
            var appKey = Environment.GetEnvironmentVariable("DROPBOX_APP_KEY")
                ?? _configuration["Dropbox:AppKey"];

            if (string.IsNullOrEmpty(appKey))
                throw new Exception("Dropbox App Key not configured");

            return appKey;
        }

        private string GetAppSecret()
        {
            var appSecret = Environment.GetEnvironmentVariable("DROPBOX_APP_SECRET")
                ?? _configuration["Dropbox:AppSecret"];

            if (string.IsNullOrEmpty(appSecret))
                throw new Exception("Dropbox App Secret not configured");

            return appSecret;
        }

        private string GetRedirectUri()
        {
            var redirectUri = Environment.GetEnvironmentVariable("DROPBOX_REDIRECT_URI")
                ?? _configuration["Dropbox:RedirectUri"];

            if (string.IsNullOrEmpty(redirectUri))
                throw new Exception("Dropbox Redirect URI not configured");

            return redirectUri;
        }
    }
}
