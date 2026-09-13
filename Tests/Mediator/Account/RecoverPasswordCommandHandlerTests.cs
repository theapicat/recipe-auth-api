using Application.Mediator.Account.Commands.RecoverPassword;
using Contracts.Events.UserActions;
using Domain.Options;
using MassTransit;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Tests.Support;

namespace Tests.Mediator.Account;

public class RecoverPasswordCommandHandlerTests : IDisposable
{
    private readonly HandlerTestHarness _harness = new();
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();
    private readonly IOptions<AppSettings> _appSettings = Options.Create(new AppSettings { FrontendUrl = "http://localhost:3000" });

    private RecoverPasswordCommandHandler CreateHandler() =>
        new(_harness.UserManager, _publishEndpoint, _appSettings);

    [Fact]
    public async Task Handle_ExistingEmail_PublishesResetEvent()
    {
        var user = await _harness.CreateUserAsync("finnes@test.local");
        var handler = CreateHandler();

        var result = await handler.Handle(new RecoverPasswordCommand("finnes@test.local"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _publishEndpoint.Received(1).Publish(
            Arg.Is<PasswordResetRequestedEvent>(e => e.UserId == user.Id), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Sikkerhetsegenskap: svaret skal være identisk (Success, samme melding) uavhengig av om
    /// e-posten faktisk finnes, slik at endepunktet ikke kan brukes til å kartlegge hvilke
    /// e-postadresser som er registrert (user enumeration).
    /// </summary>
    [Fact]
    public async Task Handle_UnknownEmail_StillReturnsSuccessButPublishesNoEvent()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new RecoverPasswordCommand("finnes-ikke@test.local"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _publishEndpoint.DidNotReceive()
            .Publish(Arg.Any<PasswordResetRequestedEvent>(), Arg.Any<CancellationToken>());
    }

    public void Dispose() => _harness.Dispose();
}
