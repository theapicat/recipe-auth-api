using Application.Mediator.Account.Commands.Register;
using Contracts.Events.UserActions;
using Domain.Entities;
using Domain.Enums;
using Domain.Options;
using MassTransit;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Tests.Support;

namespace Tests.Mediator.Account;

public class RegisterUserCommandHandlerTests : IDisposable
{
    private readonly HandlerTestHarness _harness = new();
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();
    private readonly IOptions<AppSettings> _appSettings = Options.Create(new AppSettings { FrontendUrl = "http://localhost:3000" });

    private RegisterUserCommandHandler CreateHandler() =>
        new(_harness.UserManager, _harness.DbContext, _publishEndpoint, _appSettings);

    [Fact]
    public async Task Handle_ValidRegistration_SucceedsAndPublishesEvent()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(
            new RegisterUserCommand("ny@test.local", "GyldigPassord1", "Ny", "Bruker"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.UserProfile.ShouldNotBeNull();
        result.UserProfile!.Email.ShouldBe("ny@test.local");

        var created = await _harness.UserManager.FindByEmailAsync("ny@test.local");
        created.ShouldNotBeNull();
        (await _harness.UserManager.IsInRoleAsync(created!, "user")).ShouldBeTrue();

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<UserRegisteredEvent>(e => e.Email == "ny@test.local"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExactEmailBlacklisted_IsRejectedBeforeAccountCreation()
    {
        _harness.DbContext.BlacklistedEntries.Add(new BlacklistedEntry
        {
            Pattern = "spammer@test.local", Type = BlacklistType.ExactEmail
        });
        await _harness.DbContext.SaveChangesAsync();

        var handler = CreateHandler();
        var result = await handler.Handle(
            new RegisterUserCommand("spammer@test.local", "GyldigPassord1", "S", "P"), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        (await _harness.UserManager.FindByEmailAsync("spammer@test.local")).ShouldBeNull();
        await _publishEndpoint.DidNotReceive().Publish(Arg.Any<UserRegisteredEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmailDomainBlacklisted_IsRejected()
    {
        _harness.DbContext.BlacklistedEntries.Add(new BlacklistedEntry
        {
            Pattern = "disposable.test", Type = BlacklistType.Domain
        });
        await _harness.DbContext.SaveChangesAsync();

        var handler = CreateHandler();
        var result = await handler.Handle(
            new RegisterUserCommand("noen@disposable.test", "GyldigPassord1", "N", "D"), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        (await _harness.UserManager.FindByEmailAsync("noen@disposable.test")).ShouldBeNull();
    }

    [Fact]
    public async Task Handle_BlacklistCheckIsCaseInsensitive()
    {
        _harness.DbContext.BlacklistedEntries.Add(new BlacklistedEntry
        {
            Pattern = "blocked.test", Type = BlacklistType.Domain
        });
        await _harness.DbContext.SaveChangesAsync();

        var handler = CreateHandler();
        var result = await handler.Handle(
            new RegisterUserCommand("Bruker@BLOCKED.TEST", "GyldigPassord1", "B", "T"), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_SubdomainOfBlacklistedDomain_IsNotBlocked()
    {
        // Domenesjekken matcher eksakt e-postdomene, ikke subdomener av det svartelistede
        // domenet. Dokumenterer dagens (tilsiktede) presise match-atferd eksplisitt.
        _harness.DbContext.BlacklistedEntries.Add(new BlacklistedEntry
        {
            Pattern = "blocked.test", Type = BlacklistType.Domain
        });
        await _harness.DbContext.SaveChangesAsync();

        var handler = CreateHandler();
        var result = await handler.Handle(
            new RegisterUserCommand("bruker@sub.blocked.test", "GyldigPassord1", "B", "T"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_EmailAlreadyRegistered_IsRejected()
    {
        await _harness.CreateUserAsync("finnes@test.local");
        var handler = CreateHandler();

        var result = await handler.Handle(
            new RegisterUserCommand("finnes@test.local", "GyldigPassord1", "F", "T"), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    public void Dispose() => _harness.Dispose();
}
