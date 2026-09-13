using Application.Mediator.Account.Commands.ResetPassword;
using Contracts.Events.UserActions;
using MassTransit;
using NSubstitute;
using Shouldly;
using Tests.Support;

namespace Tests.Mediator.Account;

public class ResetPasswordCommandHandlerTests : IDisposable
{
    private readonly HandlerTestHarness _harness = new();
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();

    private ResetPasswordCommandHandler CreateHandler() => new(_harness.UserManager, _publishEndpoint);

    [Fact]
    public async Task Handle_ValidToken_ResetsPasswordAndPublishesEvent()
    {
        var user = await _harness.CreateUserAsync("bruker@test.local", "GammeltPassord1");
        var token = await _harness.UserManager.GeneratePasswordResetTokenAsync(user);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ResetPasswordCommand("bruker@test.local", token, "NyttPassord1", "9.9.9.9", "Agent"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        (await _harness.SignInManager.CheckPasswordSignInAsync(user, "NyttPassord1", false)).Succeeded.ShouldBeTrue();

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<PasswordChangedEvent>(e => e.UserId == user.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidToken_Fails()
    {
        await _harness.CreateUserAsync("bruker@test.local", "GammeltPassord1");
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ResetPasswordCommand("bruker@test.local", "ugyldig-token", "NyttPassord1", null, null),
            CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_UnknownEmail_ReturnsNotFound()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ResetPasswordCommand("finnes-ikke@test.local", "uansett", "NyttPassord1", null, null),
            CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.IsNotFound.ShouldBeTrue();
    }

    public void Dispose() => _harness.Dispose();
}
