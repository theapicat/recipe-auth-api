using System.Net;
using System.Net.Http.Json;
using Shouldly;
using Tests.Support;

namespace Tests.Integration;

public class AuthorizationControllerTests : IClassFixture<AuthApiWebApplicationFactory>
{
    private readonly AuthApiWebApplicationFactory _factory;

    public AuthorizationControllerTests(AuthApiWebApplicationFactory factory) => _factory = factory;

    // Seedet av IdentitySeeder ved oppstart, se appsettings.json -> AdminUser.
    private const string AdminEmail = "admin@kjoekkenhylla.local";
    private const string AdminPassword = "AdminSuperSecretPassword123!";

    [Fact]
    public async Task Token_ValidPasswordGrant_ReturnsAccessAndRefreshToken()
    {
        var client = _factory.CreateClient();

        var token = await OAuthTestHelpers.RequestPasswordGrantTokenAsync(client, AdminEmail, AdminPassword);

        token.GetProperty("access_token").GetString().ShouldNotBeNullOrWhiteSpace();
        token.GetProperty("token_type").GetString().ShouldBe("Bearer");
        token.TryGetProperty("refresh_token", out var refreshToken).ShouldBeTrue();
        refreshToken.GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Token_WrongPassword_ReturnsInvalidGrantError()
    {
        var client = _factory.CreateClient();

        var response = await OAuthTestHelpers.RequestTokenAsync(client, new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = AdminEmail,
            ["password"] = "feil-passord",
            ["client_id"] = OAuthTestHelpers.WebAppClientId
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElementWrapper>();
        body!.error.ShouldBe("invalid_grant");
    }

    [Fact]
    public async Task Token_UnsupportedGrantType_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await OAuthTestHelpers.RequestTokenAsync(client, new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = OAuthTestHelpers.WebAppClientId
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Token_RefreshTokenGrant_IssuesNewAccessToken()
    {
        var client = _factory.CreateClient();
        var initial = await OAuthTestHelpers.RequestPasswordGrantTokenAsync(client, AdminEmail, AdminPassword);
        var refreshToken = initial.GetProperty("refresh_token").GetString();

        var response = await OAuthTestHelpers.RequestTokenAsync(client, new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken!,
            ["client_id"] = OAuthTestHelpers.WebAppClientId
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var renewed = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        renewed.GetProperty("access_token").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    private class JsonElementWrapper
    {
        public string? error { get; set; }
    }
}
