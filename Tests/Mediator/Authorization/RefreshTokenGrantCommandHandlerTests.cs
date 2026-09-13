using System.Security.Claims;
using Application.Mediator.Authorization.Commands.RefreshTokenGrant;
using Application.TokenService.Interfaces;
using NSubstitute;
using OpenIddict.Abstractions;
using Persistence.Context;
using Shouldly;
using Tests.Support;

namespace Tests.Mediator.Authorization;

public class RefreshTokenGrantCommandHandlerTests : IDisposable
{
    private readonly HandlerTestHarness _harness = new();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();

    private RefreshTokenGrantCommandHandler CreateHandler() =>
        new(_harness.UserManager, _harness.SignInManager, _tokenService);

    public RefreshTokenGrantCommandHandlerTests()
    {
        _tokenService.CreateClaimsPrincipalAsync(Arg.Any<ApplicationUser>())
            .Returns(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    private static ClaimsPrincipal PrincipalFor(Guid userId)
    {
        var identity = new ClaimsIdentity([new Claim(OpenIddictConstants.Claims.Subject, userId.ToString())]);
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public async Task Handle_ValidRefresh_SucceedsAndReissuesFreshPrincipal()
    {
        var user = await _harness.CreateUserAsync("bruker@test.local");
        var handler = CreateHandler();

        var result = await handler.Handle(new RefreshTokenGrantCommand(PrincipalFor(user.Id)), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var updated = await _harness.UserManager.FindByIdAsync(user.Id.ToString());
        updated!.LastLoginAt.ShouldNotBeNull();
        await _tokenService.Received(1).CreateClaimsPrincipalAsync(Arg.Is<ApplicationUser>(u => u.Id == user.Id));
    }

    [Fact]
    public async Task Handle_NullPrincipal_Fails()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new RefreshTokenGrantCommand(null), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_PrincipalMissingSubjectClaim_Fails()
    {
        var handler = CreateHandler();
        var principalWithoutSubject = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await handler.Handle(new RefreshTokenGrantCommand(principalWithoutSubject), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_UserWasDeletedAfterTokenWasIssued_Fails()
    {
        // Simulerer at brukeren fantes da refresh-tokenet ble utstedt (claim med gyldig
        // subject), men er slettet fra databasen innen tokenet brukes til fornyelse.
        var deletedUserId = Guid.NewGuid();
        var handler = CreateHandler();

        var result = await handler.Handle(new RefreshTokenGrantCommand(PrincipalFor(deletedUserId)), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_LockedOutUser_Fails()
    {
        var user = await _harness.CreateUserAsync("sperret@test.local");
        await _harness.UserManager.SetLockoutEnabledAsync(user, true);
        await _harness.UserManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

        var handler = CreateHandler();
        var result = await handler.Handle(new RefreshTokenGrantCommand(PrincipalFor(user.Id)), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    public void Dispose() => _harness.Dispose();
}
