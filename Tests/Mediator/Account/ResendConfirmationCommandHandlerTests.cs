using Application.Mediator.Account.Commands.ResendConfirmation;
using Contracts.Events.UserActions;
using Domain.Options;
using MassTransit;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Tests.Support;

namespace Tests.Mediator.Account;

public class ResendConfirmationCommandHandlerTests : IDisposable
{
    private readonly HandlerTestHarness _harness = new();
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();
    private readonly IOptions<AppSettings> _appSettings = Options.Create(new AppSettings { FrontendUrl = "http://localhost:3000" });

    private ResendConfirmationCommandHandler CreateHandler() =>
        new(_harness.UserManager, _publishEndpoint, _appSettings);

    [Fact]
    public async Task Handle_UnconfirmedUser_PublishesConfirmationEvent()
    {
        var user = await _harness.CreateUserAsync("ubekreftet@test.local", emailConfirmed: false);
        var handler = CreateHandler();

        var result = await handler.Handle(new ResendConfirmationCommand(user.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _publishEndpoint.Received(1).Publish(
            Arg.Is<ResendEmailConfirmationRequestedEvent>(e => e.UserId == user.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyConfirmedUser_IsRejected()
    {
        var user = await _harness.CreateUserAsync("alleredebekreftet@test.local", emailConfirmed: true);
        var handler = CreateHandler();

        var result = await handler.Handle(new ResendConfirmationCommand(user.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.IsAlreadyConfirmed.ShouldBeTrue();
        await _publishEndpoint.DidNotReceive()
            .Publish(Arg.Any<ResendEmailConfirmationRequestedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownUser_Fails()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new ResendConfirmationCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    public void Dispose() => _harness.Dispose();
}
