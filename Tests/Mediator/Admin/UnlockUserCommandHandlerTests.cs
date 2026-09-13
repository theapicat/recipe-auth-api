using Application.Mediator.Admin.Commands.UnlockUser;
using Contracts.Events.AdminActions;
using Domain.Enums;
using MassTransit;
using NSubstitute;
using Shouldly;
using Tests.Support;

namespace Tests.Mediator.Admin;

public class UnlockUserCommandHandlerTests : IDisposable
{
    private readonly HandlerTestHarness _harness = new();
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();

    private UnlockUserCommandHandler CreateHandler() => new(_harness.UserManager, _publishEndpoint);

    [Fact]
    public async Task Handle_LockedUser_UnlocksAndClearsReason()
    {
        var user = await _harness.CreateUserAsync("sperret@test.local");
        await _harness.UserManager.SetLockoutEnabledAsync(user, true);
        await _harness.UserManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        user.LockoutReason = LockoutReason.ManualAdminLock;
        user.LockoutReasonDetails = "Test";
        await _harness.DbContext.SaveChangesAsync();

        var handler = CreateHandler();
        var result = await handler.Handle(new UnlockUserCommand(user.Id.ToString()), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var updated = await _harness.UserManager.FindByIdAsync(user.Id.ToString());
        (await _harness.UserManager.IsLockedOutAsync(updated!)).ShouldBeFalse();
        updated!.LockoutReason.ShouldBe(LockoutReason.None);
        updated.LockoutReasonDetails.ShouldBeNull();

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<UserUnlockedByAdminEvent>(e => e.UserId == user.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownUser_ReturnsNotFound()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new UnlockUserCommand(Guid.NewGuid().ToString()), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    public void Dispose() => _harness.Dispose();
}
