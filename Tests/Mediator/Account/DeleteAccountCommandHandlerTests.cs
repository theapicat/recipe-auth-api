using Application.Mediator.Account.Commands.DeleteAccount;
using Contracts.Events.UserActions;
using MassTransit;
using NSubstitute;
using Shouldly;
using Tests.Support;

namespace Tests.Mediator.Account;

public class DeleteAccountCommandHandlerTests : IDisposable
{
    private readonly HandlerTestHarness _harness = new();
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();

    private DeleteAccountCommandHandler CreateHandler() => new(_harness.UserManager, _publishEndpoint);

    [Fact]
    public async Task Handle_RegularUser_DeletesAndPublishesEvent()
    {
        var user = await _harness.CreateUserAsync("sletter-seg-selv@test.local");
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteAccountCommand(user.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        (await _harness.UserManager.FindByIdAsync(user.Id.ToString())).ShouldBeNull();

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<UserAccountDeletedByUserEvent>(e => e.UserId == user.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SystemAdministrator_CannotSelfDeleteViaApi()
    {
        var admin = await _harness.CreateUserAsync("admin@test.local", isAdmin: true);
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteAccountCommand(admin.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.IsForbidden.ShouldBeTrue();
        (await _harness.UserManager.FindByIdAsync(admin.Id.ToString())).ShouldNotBeNull();
    }

    [Fact]
    public async Task Handle_UnknownUser_Fails()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteAccountCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    public void Dispose() => _harness.Dispose();
}
