using Application.Mediator.Admin.Commands.DeleteUserAdmin;
using Contracts.Events.AdminActions;
using MassTransit;
using NSubstitute;
using Shouldly;
using Tests.Support;

namespace Tests.Mediator.Admin;

public class DeleteUserAdminCommandHandlerTests : IDisposable
{
    private readonly HandlerTestHarness _harness = new();
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();

    private DeleteUserAdminCommandHandler CreateHandler() => new(_harness.UserManager, _publishEndpoint);

    [Fact]
    public async Task Handle_DeletingRegularUser_Succeeds()
    {
        var admin = await _harness.CreateUserAsync("admin@test.local", isAdmin: true);
        var target = await _harness.CreateUserAsync("user@test.local");
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteUserAdminCommand(target.Id.ToString(), admin.Id),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.TargetEmail.ShouldBe(target.Email);
        (await _harness.UserManager.FindByIdAsync(target.Id.ToString())).ShouldBeNull();

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<UserAccountDeletedByAdminEvent>(e => e.UserId == target.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AdminDeletesOwnAccount_IsRejected()
    {
        var admin = await _harness.CreateUserAsync("admin@test.local", isAdmin: true);
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteUserAdminCommand(admin.Id.ToString(), admin.Id),
            CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.IsBadRequest.ShouldBeTrue();
        (await _harness.UserManager.FindByIdAsync(admin.Id.ToString())).ShouldNotBeNull();
    }

    [Fact]
    public async Task Handle_AdminDeletesAnotherAdmin_IsRejected()
    {
        var actingAdmin = await _harness.CreateUserAsync("admin1@test.local", isAdmin: true);
        var otherAdmin = await _harness.CreateUserAsync("admin2@test.local", isAdmin: true);
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteUserAdminCommand(otherAdmin.Id.ToString(), actingAdmin.Id),
            CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.IsBadRequest.ShouldBeTrue();
        (await _harness.UserManager.FindByIdAsync(otherAdmin.Id.ToString())).ShouldNotBeNull();
        await _publishEndpoint.DidNotReceive()
            .Publish(Arg.Any<UserAccountDeletedByAdminEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownUserId_ReturnsNotFound()
    {
        var admin = await _harness.CreateUserAsync("admin@test.local", isAdmin: true);
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteUserAdminCommand(Guid.NewGuid().ToString(), admin.Id),
            CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.IsNotFound.ShouldBeTrue();
    }

    public void Dispose() => _harness.Dispose();
}
