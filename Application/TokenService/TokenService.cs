using System.Security.Claims;
using System.Text;
using Application.TokenService.Interfaces;
using Domain.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;
using Persistence.Context;

namespace Application.TokenService;

public class TokenService(
    UserManager<ApplicationUser> userManager,
    IOptions<JwtOptions> jwtOptions,
    IOptions<AppSettings> appSettings,
    IOpenIddictServerFactory serverFactory,
    IOpenIddictServerDispatcher serverDispatcher) : ITokenService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<ClaimsPrincipal> CreateClaimsPrincipalAsync(ApplicationUser user)
    {
        var identity = new ClaimsIdentity(
            TokenValidationParameters.DefaultAuthenticationType,
            OpenIddictConstants.Claims.Name,
            OpenIddictConstants.Claims.Role);

        // 💡 Fortell OpenIddict at dette er et Access Token
        identity.AddClaim(OpenIddictConstants.Claims.TokenType, OpenIddictConstants.TokenTypeHints.AccessToken);

        // 💡 Legg til utsteder som en standard claim i identiteten
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Issuer, _jwt.Issuer));

        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, user.Id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Email, user.Email ?? string.Empty));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.GivenName, user.FirstName ?? string.Empty));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.FamilyName, user.LastName ?? string.Empty));

        var roles = await userManager.GetRolesAsync(user);
        foreach (var role in roles) identity.AddClaim(new Claim(OpenIddictConstants.Claims.Role, role));

        // Sett destinasjon for OpenIddict sine egne valideringsregler
        identity.SetDestinations(_ => new[] { OpenIddictConstants.Destinations.AccessToken });

        var principal = new ClaimsPrincipal(identity);

        principal.SetScopes(
            OpenIddictConstants.Scopes.OpenId,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.Roles,
            OpenIddictConstants.Scopes.OfflineAccess
        );

        if (!string.IsNullOrWhiteSpace(_jwt.Audience)) principal.SetResources(_jwt.Audience);

        return principal;
    }

    public async Task<string> GenerateAccessTokenAsync(ApplicationUser user)
    {
        var principal = await CreateClaimsPrincipalAsync(user);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var issuer = _jwt.Issuer;

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = (ClaimsIdentity)principal.Identity!,
        
            // 💡 Bruker konfigurert levetid fra JwtOptions
            Expires = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenLifetimeInMinutes),
            Issuer = issuer,
            Audience = _jwt.Audience,
            SigningCredentials = credentials,
            TokenType = "at+jwt",

            Claims = new Dictionary<string, object>
            {
                { OpenIddictConstants.Claims.Issuer, issuer },
                { OpenIddictConstants.Claims.TokenType, OpenIddictConstants.TokenTypeHints.AccessToken }
            }
        };

        var handler = new JsonWebTokenHandler();
        return handler.CreateToken(tokenDescriptor);
    }

    public async Task<(string AccessToken, string RefreshToken)> IssueTokenPairAsync(ApplicationUser user, Uri baseUri)
    {
        var principal = await CreateClaimsPrincipalAsync(user);

        var transaction = await serverFactory.CreateTransactionAsync();
        transaction.EndpointType = OpenIddictServerEndpointType.Token;
        // Uten en BaseUri (og uten Options.Issuer konfigurert) kaster OpenIddicts interne
        // PrepareAccessTokenPrincipal-handler "The issuer cannot be retrieved...", siden den ikke
        // har noen ekte HTTP-forespørsel å utlede issueren fra slik den ellers ville hatt for en
        // vanlig /connect/token-forespørsel.
        transaction.BaseUri = baseUri;
        transaction.Request = new OpenIddictRequest
        {
            GrantType = OpenIddictConstants.GrantTypes.Password,
            // Knytter tokenet til recipe-web-app, slik at et senere POST /connect/revoke med
            // client_id=recipe-web-app (jf. BACKEND_REQUIREMENTS.md pkt. 2) finner samme klient
            // som tokenet ble utstedt til.
            ClientId = appSettings.Value.WebAppClientId
        };
        // ProcessSignInContext.Response leser fra transaction.Response uten null-sjekk — for en ekte
        // HTTP-forespørsel fyller OpenIddicts ASP.NET Core-vert denne inn tidlig i pipelinen. Her må vi
        // gjøre det selv, ellers kaster AttachSignInParameters en NullReferenceException når den prøver
        // å skrive access-/refresh-tokenet inn i responsen.
        transaction.Response = new OpenIddictResponse();

        var context = new ProcessSignInContext(transaction)
        {
            Principal = principal,
            AccessTokenPrincipal = principal,
            RefreshTokenPrincipal = principal,
            GenerateAccessToken = true,
            IncludeAccessToken = true,
            GenerateRefreshToken = true,
            IncludeRefreshToken = true
        };

        await serverDispatcher.DispatchAsync(context);

        if (string.IsNullOrEmpty(context.AccessToken) || string.IsNullOrEmpty(context.RefreshToken))
            throw new InvalidOperationException(
                "OpenIddict genererte ikke access-/refresh-token for Google-innloggingen.");

        return (context.AccessToken, context.RefreshToken);
    }
}
