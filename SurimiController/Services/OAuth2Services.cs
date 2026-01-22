namespace SurimiController.Services
{
    public class OAuth2Services : IOAuth2Services
    {
        public async Task<string> GetAccessTokenAsync(string? username, string? password)
        {
            if(string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Username and password must be provided to get the OAuth2 access token");
            }

            using var http = new HttpClient();

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("client_id", "onyxia-minio"),
                new KeyValuePair<string,string>("grant_type", "password"),
                new KeyValuePair<string,string>("scope", "openid email profile"),
                new KeyValuePair<string,string>("username", username),
                new KeyValuePair<string,string>("password", password)
            });

            var resp = await http.PostAsync(
                "https://auth.dive.edito.eu/auth/realms/datalab/protocol/openid-connect/token",
                content);

            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadAsStringAsync();
            var doc = System.Text.Json.JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("access_token").GetString()!;
        }
    }
}
