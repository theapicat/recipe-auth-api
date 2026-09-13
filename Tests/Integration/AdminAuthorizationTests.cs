using System.Net;
using System.Net.Http.Headers;
using Shouldly;
using Tests.Support;

namespace Tests.Integration;

public class AdminAuthorizationTests : IClassFixture<AuthApiWebApplicationFactory>
{
    private readonly AuthApiWebApplicationFactory _factory;

    public AdminAuthorizationTests(AuthApiWebApplicationFactory factory) => _factory = factory;

    private const string AdminEmail = "admin@kjoekkenhylla.local";
    private const string AdminPassword = "AdminSuperSecretPassword123!";

    private static async Task<string> RegisterAndGetAccessTokenAsync(HttpClient client, string email)
    {
        const string password = "TestPass1!"; // oppfyller RegisterRequest sitt strengere regex-krav

        var registerResponse = await client.PostAsync("/api/auth/account/register", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Email"] = email,
                ["Password"] = password,
                ["FirstName"] = "Test",
                ["LastName"] = "Bruker"
            }));
        registerResponse.EnsureSuccessStatusCode();

        var token = await OAuthTestHelpers.RequestPasswordGrantTokenAsync(client, email, password);
        return token.GetProperty("access_token").GetString()!;
    }

    [Fact]
    public async Task GetUsers_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/admin/users");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUsers_WithRegularUserToken_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        var accessToken = await RegisterAndGetAccessTokenAsync(client, "vanligbruker@test.local");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await client.GetAsync("/api/auth/admin/users");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUsers_WithAdminToken_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var token = await OAuthTestHelpers.RequestPasswordGrantTokenAsync(client, AdminEmail, AdminPassword);
        var accessToken = token.GetProperty("access_token").GetString();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await client.GetAsync("/api/auth/admin/users");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProfile_WithValidUserToken_ReturnsOwnProfile()
    {
        var client = _factory.CreateClient();
        var accessToken = await RegisterAndGetAccessTokenAsync(client, "profilbruker@test.local");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await client.GetAsync("/api/auth/account/me");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("profilbruker@test.local");
    }
}
