using Application.Mediator.Admin.Commands.UpdateUser;
using Contracts.Events.AdminActions;
using MassTransit;
using NSubstitute;
using Shouldly;
using Tests.Support;

namespace Tests.Mediator.Admin;

public class AdminUpdateUserCommandHandlerTests : IDisposable
{
    private readonly HandlerTestHarness _harness = new();
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();

    private AdminUpdateUserCommandHandler CreateHandler() => new(_harness.UserManager, _publishEndpoint);

    [Fact]
    public async Task Handle_UpdatingRegularUser_Succeeds()
    {
        var admin = await _harness.CreateUserAsync("admin@test.local", isAdmin: true);
        var target = await _harness.CreateUserAsync("olduser@test.local");
        var handler = CreateHandler();

        var result = await handler.Handle(
            new AdminUpdateUserCommand(target.Id.ToString(), "newmail@test.local", "Nytt", "Navn", admin.Id),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var updated = await _harness.UserManager.FindByIdAsync(target.Id.ToString());
        updated!.Email.ShouldBe("newmail@test.local");
        updated.FirstName.ShouldBe("Nytt");

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<UserUpdatedByAdminEvent>(e => e.UserId == target.Id && e.OldEmail == "olduser@test.local"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AdminEditsOwnAccount_IsRejected()
    {
        var admin = await _harness.CreateUserAsync("admin@test.local", isAdmin: true);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new AdminUpdateUserCommand(admin.Id.ToString(), "hacked@test.local", "X", "Y", admin.Id),
            CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.IsBadRequest.ShouldBeTrue();

        var unchanged = await _harness.UserManager.FindByIdAsync(admin.Id.ToString());
        unchanged!.Email.ShouldBe("admin@test.local");
    }

    [Fact]
    public async Task Handle_AdminEditsAnotherAdmin_IsRejected()
    {
        var actingAdmin = await _harness.CreateUserAsync("admin1@test.local", isAdmin: true);
        var otherAdmin = await _harness.CreateUserAsync("admin2@test.local", isAdmin: true);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new AdminUpdateUserCommand(otherAdmin.Id.ToString(), "hijacked@test.local", "X", "Y", actingAdmin.Id),
            CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.IsBadRequest.ShouldBeTrue();

        var unchanged = await _harness.UserManager.FindByIdAsync(otherAdmin.Id.ToString());
        unchanged!.Email.ShouldBe("admin2@test.local");
        await _publishEndpoint.DidNotReceive().Publish(Arg.Any<UserUpdatedByAdminEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownUserId_ReturnsNotFound()
    {
        var admin = await _harness.CreateUserAsync("admin@test.local", isAdmin: true);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new AdminUpdateUserCommand(Guid.NewGuid().ToString(), "x@test.local", "X", "Y", admin.Id),
            CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.IsNotFound.ShouldBeTrue();
    }

    public void Dispose() => _harness.Dispose();
}
