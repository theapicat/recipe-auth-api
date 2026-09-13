using Application.Mediator.Account.Commands.ChangePassword;
using Contracts.Events.UserActions;
using MassTransit;
using NSubstitute;
using Shouldly;
using Tests.Support;

namespace Tests.Mediator.Account;

public class ChangePasswordCommandHandlerTests : IDisposable
{
    private readonly HandlerTestHarness _harness = new();
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();

    private ChangePasswordCommandHandler CreateHandler() => new(_harness.UserManager, _publishEndpoint);

    [Fact]
    public async Task Handle_CorrectCurrentPassword_SucceedsAndPublishesEvent()
    {
        var user = await _harness.CreateUserAsync("bruker@test.local", "GammeltPassord1");
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ChangePasswordCommand(user.Id, "GammeltPassord1", "NyttPassord1", "1.2.3.4", "TestAgent"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        (await _harness.SignInManager.CheckPasswordSignInAsync(user, "NyttPassord1", false)).Succeeded.ShouldBeTrue();

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<PasswordChangedEvent>(e => e.UserId == user.Id && e.IpAddress == "1.2.3.4"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WrongCurrentPassword_Fails()
    {
        var user = await _harness.CreateUserAsync("bruker@test.local", "GammeltPassord1");
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ChangePasswordCommand(user.Id, "FeilPassord1", "NyttPassord1", null, null), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        await _publishEndpoint.DidNotReceive().Publish(Arg.Any<PasswordChangedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownUser_Fails()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ChangePasswordCommand(Guid.NewGuid(), "X", "NyttPassord1", null, null), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    public void Dispose() => _harness.Dispose();
}
