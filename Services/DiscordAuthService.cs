using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;

namespace EAPlaymateGroup.Services;

public sealed class DiscordAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public DiscordAuthService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret);

    public string BuildAuthorizationUrl(string redirectUri, string state, string prompt = "none")
    {
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = ClientId,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["scope"] = "identify",
            ["state"] = state,
            ["prompt"] = prompt
        };

        return QueryHelpers.AddQueryString("https://discord.com/oauth2/authorize", query);
    }

    public async Task<DiscordUserProfile> GetUserProfileAsync(string code, string redirectUri)
    {
        using var tokenResponse = await _httpClient.PostAsync(
            "https://discord.com/api/oauth2/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = ClientId,
                ["client_secret"] = ClientSecret,
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri
            }));
        tokenResponse.EnsureSuccessStatusCode();

        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
        var token = JsonSerializer.Deserialize<DiscordTokenResponse>(tokenJson, JsonOptions)
            ?? throw new InvalidOperationException("Discord token response is empty.");
        if (string.IsNullOrWhiteSpace(token.AccessToken))
        {
            throw new InvalidOperationException("Discord access token is missing.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/users/@me");
        request.Headers.Authorization = new("Bearer", token.AccessToken);
        using var userResponse = await _httpClient.SendAsync(request);
        userResponse.EnsureSuccessStatusCode();

        var userJson = await userResponse.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<DiscordUserProfile>(userJson, JsonOptions)
            ?? throw new InvalidOperationException("Discord user response is empty.");
    }

    private string ClientId => _configuration["Discord:ClientId"] ?? string.Empty;
    private string ClientSecret => _configuration["Discord:ClientSecret"] ?? string.Empty;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed class DiscordTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
    }
}

public sealed class DiscordUserProfile
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("global_name")]
    public string? GlobalName { get; set; }
}
