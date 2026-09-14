using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.TokenService.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Context;
using Shouldly;
using Tests.Support;

namespace Tests.Integration;

/// <summary>
/// Lag 4-test som kjører TokenService.IssueTokenPairAsync (Google-callback-flytens token-
/// utstedelse, se Application/TokenService/TokenService.cs) mot den EKTE, fullt oppstartede
/// OpenIddict-pipelinen — samme vert som AuthorizationControllerTests bruker for password-/
/// refresh-grant-flyten.
///
/// Dette er bevisst IKKE en mocket enhetstest: to reelle runtime-feil i denne pipelinen
/// (manglende issuer i PrepareAccessTokenPrincipal, manglende Response-objekt i
/// AttachSignInParameters) ble først oppdaget ved manuell Google-innlogging i nettleseren,
/// fordi ProcessGoogleCallbackCommandHandlerTests mocker ITokenService bort og dermed aldri
/// faktisk kjører OpenIddicts interne handler-kjede. Denne testen kjører den kjeden for ekte,
/// slik at denne typen feil fanges her fremover i stedet for i produksjon.
/// </summary>
public class TokenServiceIssueTokenPairTests : IClassFixture<AuthApiWebApplicationFactory>
{
    private readonly AuthApiWebApplicationFactory _factory;

    public TokenServiceIssueTokenPairTests(AuthApiWebApplicationFactory factory) => _factory = factory;

    private async Task<ApplicationUser> CreateTestUserAsync(UserManager<ApplicationUser> userManager, string tag)
    {
        var email = $"google-pipeline-{tag}-{Guid.NewGuid():N}@test.local";
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Google",
            LastName = "Bruker",
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow
        };

        (await userManager.CreateAsync(user)).Succeeded.ShouldBeTrue();
        (await userManager.AddToRoleAsync(user, "user")).Succeeded.ShouldBeTrue();

        return user;
    }

    [Fact]
    public async Task IssueTokenPairAsync_ForRealUser_ReturnsAccessAndWorkingRefreshToken()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var user = await CreateTestUserAsync(userManager, "refresh");

        var (accessToken, refreshToken) = await tokenService.IssueTokenPairAsync(user, _factory.Server.BaseAddress);

        accessToken.ShouldNotBeNullOrWhiteSpace();
        refreshToken.ShouldNotBeNullOrWhiteSpace();

        // Selve poenget: refresh-tokenet skal faktisk kunne løses inn mot /connect/token,
        // akkurat slik frontend gjør etter en Google-innlogging (se BACKEND_REQUIREMENTS.md).
        var client = _factory.CreateClient();
        var response = await OAuthTestHelpers.RequestTokenAsync(client, new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = OAuthTestHelpers.WebAppClientId
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var renewed = await response.Content.ReadFromJsonAsync<JsonElement>();
        renewed.GetProperty("access_token").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task IssueTokenPairAsync_ThenRevoke_MakesRefreshTokenUnusable()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var user = await CreateTestUserAsync(userManager, "revoke");

        var (_, refreshToken) = await tokenService.IssueTokenPairAsync(user, _factory.Server.BaseAddress);

        var client = _factory.CreateClient();

        var revokeResponse = await client.PostAsync("/api/auth/connect/revoke", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["token"] = refreshToken,
                ["token_type_hint"] = "refresh_token",
                ["client_id"] = OAuthTestHelpers.WebAppClientId
            }));

        revokeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var refreshAttempt = await OAuthTestHelpers.RequestTokenAsync(client, new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = OAuthTestHelpers.WebAppClientId
        });

        refreshAttempt.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
