using Application.Mediator.Account.Commands.ConfirmEmail;
using Domain.Enums;
using Shouldly;
using Tests.Support;

namespace Tests.Mediator.Account;

public class ConfirmEmailCommandHandlerTests : IDisposable
{
    private readonly HandlerTestHarness _harness = new();

    private ConfirmEmailCommandHandler CreateHandler() => new(_harness.UserManager);

    [Fact]
    public async Task Handle_ValidToken_ConfirmsEmail()
    {
        var user = await _harness.CreateUserAsync("bekreft@test.local", emailConfirmed: false);
        var token = await _harness.UserManager.GenerateEmailConfirmationTokenAsync(user);
        var handler = CreateHandler();

        var result = await handler.Handle(new ConfirmEmailCommand(user.Id.ToString(), token), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var updated = await _harness.UserManager.FindByIdAsync(user.Id.ToString());
        updated!.EmailConfirmed.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_ConfirmingRemovesLockoutFromUnconfirmedEmailLock()
    {
        // Speiler AccountLifecycleJob sin 14-dagers sperring: bekreftelse etterpå skal
        // automatisk oppheve sperren, ikke la brukeren stå fanget sperret for alltid.
        var user = await _harness.CreateUserAsync("gjenopprettet@test.local", emailConfirmed: false);
        await _harness.UserManager.SetLockoutEnabledAsync(user, true);
        await _harness.UserManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        user.LockoutReason = LockoutReason.UnconfirmedEmail14Days;
        await _harness.DbContext.SaveChangesAsync();

        var token = await _harness.UserManager.GenerateEmailConfirmationTokenAsync(user);
        var handler = CreateHandler();

        var result = await handler.Handle(new ConfirmEmailCommand(user.Id.ToString(), token), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var updated = await _harness.UserManager.FindByIdAsync(user.Id.ToString());
        (await _harness.UserManager.IsLockedOutAsync(updated!)).ShouldBeFalse();
        updated!.LockoutReason.ShouldBe(LockoutReason.None);
    }

    [Fact]
    public async Task Handle_InvalidToken_Fails()
    {
        var user = await _harness.CreateUserAsync("feiltoken@test.local", emailConfirmed: false);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ConfirmEmailCommand(user.Id.ToString(), "helt-feil-token"), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        var updated = await _harness.UserManager.FindByIdAsync(user.Id.ToString());
        updated!.EmailConfirmed.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_UnknownUserId_ReturnsNotFound()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ConfirmEmailCommand(Guid.NewGuid().ToString(), "uansett"), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.IsNotFound.ShouldBeTrue();
    }

    public void Dispose() => _harness.Dispose();
}
