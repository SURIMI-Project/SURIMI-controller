using SURIMI_gui.Components;
using SURIMI_gui.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SURIMI_gui.Services
{
    public class EditoDataLabService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<EditoDataLabService> _logger;
        private string? _accessToken;
        private DateTime _tokenExpiration;

        public EditoDataLabService(HttpClient httpClient, ILogger<EditoDataLabService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            // Return cached token if still valid
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiration)
            {
                return _accessToken;
            }

            var clientId = "onyxia";
            var username = Environment.GetEnvironmentVariable("DATALAB_USERNAME");
            var password = Environment.GetEnvironmentVariable("DATALAB_PASSWORD");

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                throw new InvalidOperationException("EDITO DataLab configuration is missing. Please configure ClientId, Username, and Password.");
            }

            var tokenUrl = "https://auth.dive.edito.eu/auth/realms/datalab/protocol/openid-connect/token";

            var requestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password),
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("scope", "openid")
            });

            try
            {
                var response = await _httpClient.PostAsync(tokenUrl, requestContent, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var authResponse = JsonSerializer.Deserialize<EditoAuthResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (authResponse == null || string.IsNullOrEmpty(authResponse.access_token))
                {
                    throw new InvalidOperationException("Failed to retrieve access token from EDITO DataLab.");
                }

                _accessToken = authResponse.access_token;
                _tokenExpiration = DateTime.UtcNow.AddSeconds(authResponse.expires_in - 60); // Refresh 60 seconds before expiration

                _logger.LogInformation("Successfully authenticated with EDITO DataLab");

                return _accessToken;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to authenticate with EDITO DataLab");
                throw new InvalidOperationException("Failed to authenticate with EDITO DataLab. Please check your credentials.", ex);
            }
        }

        public async Task<List<Models.App>> GetMyLabServicesAsync(CancellationToken cancellationToken = default)
        {
            var token = await GetAccessTokenAsync(cancellationToken);

            var servicesUrl = "https://datalab.dive.edito.eu/api/my-lab/services";

            var request = new HttpRequestMessage(HttpMethod.Get, servicesUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var rootobject = JsonSerializer.Deserialize<Rootobject>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation("Successfully retrieved {Count} services from EDITO DataLab", rootobject?.apps.Length ?? 0);

                return rootobject.apps.Where(a => a.id.StartsWith("surimi")).ToList();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to retrieve services from EDITO DataLab");
                throw new InvalidOperationException("Failed to retrieve services from EDITO DataLab.", ex);
            }
        }
    }
}
