using System.Net.Http.Json;
using System.Text.Json;

namespace Tests.Support;

/// <summary>Små hjelpere for å prate OAuth2/OpenIddict-protokoll mot testverten i Lag 4-tester.</summary>
public static class OAuthTestHelpers
{
    public const string WebAppClientId = "recipe-web-app";

    public static async Task<HttpResponseMessage> RequestTokenAsync(
        HttpClient client, Dictionary<string, string> form)
    {
        using var content = new FormUrlEncodedContent(form);
        return await client.PostAsync("/api/auth/connect/token", content);
    }

    public static async Task<JsonElement> RequestPasswordGrantTokenAsync(
        HttpClient client, string username, string password, string clientId = WebAppClientId)
    {
        var response = await RequestTokenAsync(client, new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = username,
            ["password"] = password,
            ["client_id"] = clientId
        });

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
