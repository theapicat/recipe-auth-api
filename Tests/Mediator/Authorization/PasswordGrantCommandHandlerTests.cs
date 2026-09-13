using System.Security.Claims;
using Application.Mediator.Authorization.Commands.PasswordGrant;
using Application.TokenService.Interfaces;
using NSubstitute;
using Shouldly;
using Tests.Support;

namespace Tests.Mediator.Authorization;

public class PasswordGrantCommandHandlerTests : IDisposable
{
    private readonly HandlerTestHarness _harness = new();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();

    private PasswordGrantCommandHandler CreateHandler() =>
        new(_harness.UserManager, _harness.SignInManager, _tokenService);

    public PasswordGrantCommandHandlerTests()
    {
        _tokenService.CreateClaimsPrincipalAsync(Arg.Any<Persistence.Context.ApplicationUser>())
            .Returns(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    [Fact]
    public async Task Handle_ValidCredentials_SucceedsAndUpdatesLastLoginAt()
    {
        var user = await _harness.CreateUserAsync("bruker@test.local", "GyldigPassord1");
        var handler = CreateHandler();

        var result = await handler.Handle(new PasswordGrantCommand("bruker@test.local", "GyldigPassord1"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Principal.ShouldNotBeNull();

        var updated = await _harness.UserManager.FindByIdAsync(user.Id.ToString());
        updated!.LastLoginAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Handle_WrongPassword_Fails()
    {
        await _harness.CreateUserAsync("bruker@test.local", "GyldigPassord1");
        var handler = CreateHandler();

        var result = await handler.Handle(new PasswordGrantCommand("bruker@test.local", "FeilPassord1"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_UnknownUsername_Fails()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new PasswordGrantCommand("finnes-ikke@test.local", "GyldigPassord1"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_LockedOutAccount_IsRejectedEvenWithCorrectPassword()
    {
        var user = await _harness.CreateUserAsync("sperret@test.local", "GyldigPassord1");
        await _harness.UserManager.SetLockoutEnabledAsync(user, true);
        await _harness.UserManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

        var handler = CreateHandler();
        var result = await handler.Handle(new PasswordGrantCommand("sperret@test.local", "GyldigPassord1"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    public void Dispose() => _harness.Dispose();
}
