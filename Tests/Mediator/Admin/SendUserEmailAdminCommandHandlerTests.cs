using Application.Mediator.Admin.Commands.SendUserEmailAdmin;
using Contracts.Events.AdminActions;
using MassTransit;
using NSubstitute;
using Shouldly;
using Tests.Support;

namespace Tests.Mediator.Admin;

public class SendUserEmailAdminCommandHandlerTests : IDisposable
{
    private readonly HandlerTestHarness _harness = new();
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();

    private SendUserEmailAdminCommandHandler CreateHandler() => new(_harness.UserManager, _publishEndpoint);

    [Fact]
    public async Task Handle_ValidRequest_PublishesCustomEmailEvent()
    {
        var admin = await _harness.CreateUserAsync("admin@test.local", isAdmin: true);
        var target = await _harness.CreateUserAsync("mottaker@test.local");
        var handler = CreateHandler();

        var result = await handler.Handle(
            new SendUserEmailAdminCommand(target.Id.ToString(), "Emne", "Innhold", admin.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _publishEndpoint.Received(1).Publish(
            Arg.Is<AdminCustomEmailRequestedEvent>(e => e.UserId == target.Id && e.Subject == "Emne"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidUserId_ReturnsBadRequest()
    {
        var admin = await _harness.CreateUserAsync("admin@test.local", isAdmin: true);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new SendUserEmailAdminCommand("ikke-en-guid", "Emne", "Innhold", admin.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.IsBadRequest.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_EmptySubject_ReturnsBadRequest()
    {
        var admin = await _harness.CreateUserAsync("admin@test.local", isAdmin: true);
        var target = await _harness.CreateUserAsync("mottaker@test.local");
        var handler = CreateHandler();

        var result = await handler.Handle(
            new SendUserEmailAdminCommand(target.Id.ToString(), "  ", "Innhold", admin.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.IsBadRequest.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_EmptyMessage_ReturnsBadRequest()
    {
        var admin = await _harness.CreateUserAsync("admin@test.local", isAdmin: true);
        var target = await _harness.CreateUserAsync("mottaker@test.local");
        var handler = CreateHandler();

        var result = await handler.Handle(
            new SendUserEmailAdminCommand(target.Id.ToString(), "Emne", "   ", admin.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.IsBadRequest.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_UnknownUser_ReturnsNotFound()
    {
        var admin = await _harness.CreateUserAsync("admin@test.local", isAdmin: true);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new SendUserEmailAdminCommand(Guid.NewGuid().ToString(), "Emne", "Innhold", admin.Id),
            CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.IsNotFound.ShouldBeTrue();
    }

    public void Dispose() => _harness.Dispose();
}
