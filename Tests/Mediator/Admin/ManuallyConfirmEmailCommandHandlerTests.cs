using Application.Mediator.Admin.Commands.ManuallyConfirmEmail;
using Contracts.Events.AdminActions;
using Domain.Enums;
using MassTransit;
using NSubstitute;
using Shouldly;
using Tests.Support;

namespace Tests.Mediator.Admin;

public class ManuallyConfirmEmailCommandHandlerTests : IDisposable
{
    private readonly HandlerTestHarness _harness = new();
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();

    private ManuallyConfirmEmailCommandHandler CreateHandler() => new(_harness.UserManager, _publishEndpoint);

    [Fact]
    public async Task Handle_UnconfirmedUser_ConfirmsAndPublishesEvent()
    {
        var user = await _harness.CreateUserAsync("ubekreftet@test.local", emailConfirmed: false);
        var handler = CreateHandler();

        var result = await handler.Handle(new ManuallyConfirmEmailCommand(user.Id.ToString()), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var updated = await _harness.UserManager.FindByIdAsync(user.Id.ToString());
        updated!.EmailConfirmed.ShouldBeTrue();

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<EmailManuallyConfirmedByAdminEvent>(e => e.UserId == user.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserLockedForUnconfirmedEmail_UnlocksWhenManuallyConfirmed()
    {
        var user = await _harness.CreateUserAsync("sperretubekreftet@test.local", emailConfirmed: false);
        await _harness.UserManager.SetLockoutEnabledAsync(user, true);
        await _harness.UserManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        user.LockoutReason = LockoutReason.UnconfirmedEmail14Days;
        await _harness.DbContext.SaveChangesAsync();

        var handler = CreateHandler();
        var result = await handler.Handle(new ManuallyConfirmEmailCommand(user.Id.ToString()), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var updated = await _harness.UserManager.FindByIdAsync(user.Id.ToString());
        (await _harness.UserManager.IsLockedOutAsync(updated!)).ShouldBeFalse();
        updated!.LockoutReason.ShouldBe(LockoutReason.None);
    }

    [Fact]
    public async Task Handle_AlreadyConfirmedUser_IsRejected()
    {
        var user = await _harness.CreateUserAsync("bekreftet@test.local");
        var handler = CreateHandler();

        var result = await handler.Handle(new ManuallyConfirmEmailCommand(user.Id.ToString()), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.IsAlreadyConfirmed.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_UnknownUser_ReturnsNotFound()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ManuallyConfirmEmailCommand(Guid.NewGuid().ToString()), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.IsNotFound.ShouldBeTrue();
    }

    public void Dispose() => _harness.Dispose();
}
