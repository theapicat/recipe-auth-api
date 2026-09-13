using Application.Mediator.Admin.Commands.LockUser;
using Contracts.Events.AdminActions;
using MassTransit;
using NSubstitute;
using Shouldly;
using Tests.Support;

namespace Tests.Mediator.Admin;

public class LockUserCommandHandlerTests : IDisposable
{
    private readonly HandlerTestHarness _harness = new();
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();

    private LockUserCommandHandler CreateHandler() => new(_harness.UserManager, _publishEndpoint);

    [Fact]
    public async Task Handle_LockingRegularUser_Succeeds()
    {
        var admin = await _harness.CreateUserAsync("admin@test.local", isAdmin: true);
        var target = await _harness.CreateUserAsync("user@test.local");
        var handler = CreateHandler();

        var result = await handler.Handle(
            new LockUserCommand(target.Id.ToString(), "Brøt brukervilkårene", admin.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.TargetEmail.ShouldBe(target.Email);
        (await _harness.UserManager.IsLockedOutAsync(target)).ShouldBeTrue();

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<UserLockedByAdminEvent>(e => e.UserId == target.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AdminLocksOwnAccount_IsRejected()
    {
        var admin = await _harness.CreateUserAsync("admin@test.local", isAdmin: true);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new LockUserCommand(admin.Id.ToString(), null, admin.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.IsBadRequest.ShouldBeTrue();
        (await _harness.UserManager.IsLockedOutAsync(admin)).ShouldBeFalse();
        await _publishEndpoint.DidNotReceive().Publish(Arg.Any<UserLockedByAdminEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AdminLocksAnotherAdmin_IsRejected()
    {
        var actingAdmin = await _harness.CreateUserAsync("admin1@test.local", isAdmin: true);
        var otherAdmin = await _harness.CreateUserAsync("admin2@test.local", isAdmin: true);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new LockUserCommand(otherAdmin.Id.ToString(), null, actingAdmin.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.IsBadRequest.ShouldBeTrue();
        (await _harness.UserManager.IsLockedOutAsync(otherAdmin)).ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_UnknownUserId_ReturnsNotFound()
    {
        var admin = await _harness.CreateUserAsync("admin@test.local", isAdmin: true);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new LockUserCommand(Guid.NewGuid().ToString(), null, admin.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.IsNotFound.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_InvalidUserIdFormat_ReturnsBadRequest()
    {
        var admin = await _harness.CreateUserAsync("admin@test.local", isAdmin: true);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new LockUserCommand("not-a-guid", null, admin.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.IsBadRequest.ShouldBeTrue();
    }

    public void Dispose() => _harness.Dispose();
}
