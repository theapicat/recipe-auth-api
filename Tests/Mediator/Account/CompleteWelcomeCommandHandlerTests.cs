using Application.Mediator.Account.Commands.CompleteWelcome;
using Shouldly;
using Tests.Support;

namespace Tests.Mediator.Account;

public class CompleteWelcomeCommandHandlerTests : IDisposable
{
    private readonly HandlerTestHarness _harness = new();

    private CompleteWelcomeCommandHandler CreateHandler() => new(_harness.UserManager);

    [Fact]
    public async Task Handle_ExistingUser_MarksWelcomeCompletedAndReturnsProfile()
    {
        var user = await _harness.CreateUserAsync("ny@test.local");
        var handler = CreateHandler();

        var result = await handler.Handle(new CompleteWelcomeCommand(user.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.UserProfile!.WelcomeCompleted.ShouldBeTrue();

        var updated = await _harness.UserManager.FindByIdAsync(user.Id.ToString());
        updated!.WelcomeCompleted.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_UnknownUser_Fails()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new CompleteWelcomeCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    public void Dispose() => _harness.Dispose();
}
