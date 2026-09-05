using System.Security.Claims;
using System.Text;
using Application.TokenService.Interfaces;
using Domain.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using Persistence.Context;

namespace Application.TokenService;

public class TokenService(
    UserManager<ApplicationUser> userManager,
    IOptions<JwtOptions> jwtOptions) : ITokenService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<ClaimsPrincipal> CreateClaimsPrincipalAsync(ApplicationUser user)
    {
        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: OpenIddictConstants.Claims.Name,
            roleType: OpenIddictConstants.Claims.Role);

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
        foreach (var role in roles)
        {
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Role, role));
        }

        // Sett destinasjon for OpenIddict sine egne valideringsregler
        identity.SetDestinations(_ => new[] { OpenIddictConstants.Destinations.AccessToken });

        var principal = new ClaimsPrincipal(identity);

        principal.SetScopes(
            OpenIddictConstants.Scopes.OpenId,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.Roles,
            OpenIddictConstants.Scopes.OfflineAccess
        );

        if (!string.IsNullOrWhiteSpace(_jwt.Audience))
        {
            principal.SetResources(_jwt.Audience);
        }

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
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = issuer,
            Audience = _jwt.Audience,
            SigningCredentials = credentials,
        
            // 💡 1. TVING RIKTIG TYPE INN I JWT-HEADEREN (Løser ID2089)
            // OpenIddict Validation forventer typen "at+jwt" for et Access Token
            TokenType = "at+jwt", 

            // 💡 2. TVING INN OPENIDDICT SINE INTERNE CLAIMS
            Claims = new Dictionary<string, object>
            {
                { OpenIddictConstants.Claims.Issuer, issuer },
                // Denne sikrer at den også ligger i selve payloaden
                { OpenIddictConstants.Claims.TokenType, OpenIddictConstants.TokenTypeHints.AccessToken } 
            }
        };

        var handler = new JsonWebTokenHandler();
        return handler.CreateToken(tokenDescriptor);
    }

}
