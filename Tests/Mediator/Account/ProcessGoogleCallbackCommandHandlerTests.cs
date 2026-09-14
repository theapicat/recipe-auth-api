using System.Security.Claims;
using Application.Mediator.Account.Commands.GoogleCallback;
using Application.TokenService.Interfaces;
using Contracts.Events.UserActions;
using Domain.Entities;
using Domain.Enums;
using Domain.Options;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NSubstitute;
using Persistence.Context;
using Shouldly;
using Tests.Support;

namespace Tests.Mediator.Account;

public class ProcessGoogleCallbackCommandHandlerTests : IDisposable
{
    private readonly HandlerTestHarness _harness = new();
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IOptions<AppSettings> _appSettings = Options.Create(new AppSettings { FrontendUrl = "http://localhost:3000" });
    private static readonly Uri TestBaseUri = new("https://localhost:7001");

    private ProcessGoogleCallbackCommandHandler CreateHandler() => new(
        _harness.UserManager, _harness.SignInManager, _harness.DbContext, _publishEndpoint,
        _appSettings, _tokenService);

    public ProcessGoogleCallbackCommandHandlerTests()
    {
        _tokenService.IssueTokenPairAsync(Arg.Any<ApplicationUser>(), Arg.Any<Uri>())
            .Returns(("fake-access-token", "fake-refresh-token"));
    }

    private static ExternalLoginInfo GoogleInfo(string email, string providerKey = "google-sub-123",
        string firstName = "Ola", string lastName = "Nordmann")
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.GivenName, firstName),
            new Claim(ClaimTypes.Surname, lastName)
        ]);
        return new ExternalLoginInfo(new ClaimsPrincipal(identity), "Google", providerKey, "Google");
    }

    [Fact]
    public async Task Handle_BlacklistedExactEmail_RejectsBeforeTouchingUserStore()
    {
        _harness.DbContext.BlacklistedEntries.Add(new BlacklistedEntry
        {
            Pattern = "svartelistet@test.local", Type = BlacklistType.ExactEmail
        });
        await _harness.DbContext.SaveChangesAsync();

        var handler = CreateHandler();
        var result = await handler.Handle(
            new ProcessGoogleCallbackCommand(GoogleInfo("svartelistet@test.local"), null, TestBaseUri), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.RedirectUrl!.ShouldContain("error=blacklisted");

        // Selve poenget med testen: svartelistesjekken skjer FØR noen bruker opprettes/slås opp.
        (await _harness.UserManager.FindByEmailAsync("svartelistet@test.local")).ShouldBeNull();
        await _publishEndpoint.DidNotReceive()
            .Publish(Arg.Any<UserRegisteredWithGoogleEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BlacklistedDomain_RejectsBeforeTouchingUserStore()
    {
        _harness.DbContext.BlacklistedEntries.Add(new BlacklistedEntry
        {
            Pattern = "svartedomene.test", Type = BlacklistType.Domain
        });
        await _harness.DbContext.SaveChangesAsync();

        var handler = CreateHandler();
        var result = await handler.Handle(
            new ProcessGoogleCallbackCommand(GoogleInfo("noen@svartedomene.test"), null, TestBaseUri), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        (await _harness.UserManager.FindByEmailAsync("noen@svartedomene.test")).ShouldBeNull();
    }

    [Fact]
    public async Task Handle_RemoteErrorFromGoogle_RejectsImmediately()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ProcessGoogleCallbackCommand(null, "access_denied", TestBaseUri), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.RedirectUrl!.ShouldContain("error=access_denied");
    }

    [Fact]
    public async Task Handle_NewUser_CreatesAccountWithConfirmedEmailAndPublishesEvent()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ProcessGoogleCallbackCommand(GoogleInfo("nygoogle@test.local"), null, TestBaseUri), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        var created = await _harness.UserManager.FindByEmailAsync("nygoogle@test.local");
        created.ShouldNotBeNull();
        created!.EmailConfirmed.ShouldBeTrue("Google har allerede verifisert e-posten");
        (await _harness.UserManager.IsInRoleAsync(created, "user")).ShouldBeTrue();

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<UserRegisteredWithGoogleEvent>(e => e.Email == "nygoogle@test.local"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingLockedOutUser_IsRejected()
    {
        var user = await _harness.CreateUserAsync("sperretgoogle@test.local");
        await _harness.UserManager.SetLockoutEnabledAsync(user, true);
        await _harness.UserManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new ProcessGoogleCallbackCommand(GoogleInfo("sperretgoogle@test.local"), null, TestBaseUri), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.RedirectUrl!.ShouldContain("error=account_locked");
    }

    [Fact]
    public async Task Handle_ExistingUserWithoutGoogleLinkYet_LinksLoginInsteadOfCreatingDuplicate()
    {
        var user = await _harness.CreateUserAsync("koblesammen@test.local");
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ProcessGoogleCallbackCommand(GoogleInfo("koblesammen@test.local"), null, TestBaseUri), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var logins = await _harness.UserManager.GetLoginsAsync(user);
        logins.ShouldContain(l => l.LoginProvider == "Google");

        // Ingen ny bruker skal ha blitt opprettet for samme e-post
        await _publishEndpoint.DidNotReceive()
            .Publish(Arg.Any<UserRegisteredWithGoogleEvent>(), Arg.Any<CancellationToken>());
    }

    public void Dispose() => _harness.Dispose();
}
