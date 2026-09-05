using System.Security.Claims;
using Persistence.Context;

namespace Application.TokenService.Interfaces;

public interface ITokenService
{
    Task<ClaimsPrincipal> CreateClaimsPrincipalAsync(ApplicationUser user);
    Task<string> GenerateAccessTokenAsync(ApplicationUser user);
}