using Application.Mediator.Admin.Commands.SendPasswordReset;
using Contracts.Events.UserActions;
using Domain.Options;
using MassTransit;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Tests.Support;

namespace Tests.Mediator.Admin;

public class AdminSendPasswordResetCommandHandlerTests : IDisposable
{
    private readonly HandlerTestHarness _harness = new();
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();
    private readonly IOptions<AppSettings> _appSettings = Options.Create(new AppSettings { FrontendUrl = "http://localhost:3000" });

    private AdminSendPasswordResetCommandHandler CreateHandler() =>
        new(_harness.UserManager, _publishEndpoint, _appSettings);

    [Fact]
    public async Task Handle_ExistingUser_PublishesResetEvent()
    {
        var user = await _harness.CreateUserAsync("bruker@test.local");
        var handler = CreateHandler();

        var result = await handler.Handle(new AdminSendPasswordResetCommand(user.Id.ToString()), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _publishEndpoint.Received(1).Publish(
            Arg.Is<PasswordResetRequestedEvent>(e => e.UserId == user.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownUser_ReturnsNotFound()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(
            new AdminSendPasswordResetCommand(Guid.NewGuid().ToString()), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.IsNotFound.ShouldBeTrue();
    }

    public void Dispose() => _harness.Dispose();
}
