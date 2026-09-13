using Application.Mediator.Account.Commands.SetPassword;
using Contracts.Events.UserActions;
using MassTransit;
using NSubstitute;
using Persistence.Context;
using Shouldly;
using Tests.Support;

namespace Tests.Mediator.Account;

public class SetPasswordCommandHandlerTests : IDisposable
{
    private readonly HandlerTestHarness _harness = new();
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();

    private SetPasswordCommandHandler CreateHandler() => new(_harness.UserManager, _publishEndpoint);

    private async Task<ApplicationUser> CreatePasswordlessGoogleUserAsync(string email)
    {
        var user = new ApplicationUser
        {
            UserName = email, Email = email, FirstName = "Google", LastName = "Bruker",
            EmailConfirmed = true, CreatedAt = DateTime.UtcNow, LastModifiedAt = DateTime.UtcNow
        };
        var createResult = await _harness.UserManager.CreateAsync(user); // Uten passord, som Google-registrering
        createResult.Succeeded.ShouldBeTrue();
        await _harness.UserManager.AddToRoleAsync(user, "User");
        return user;
    }

    [Fact]
    public async Task Handle_GoogleOnlyUserWithoutPassword_CanSetInitialPassword()
    {
        var user = await CreatePasswordlessGoogleUserAsync("googlebruker@test.local");
        var handler = CreateHandler();

        var result = await handler.Handle(
            new SetPasswordCommand(user.Id, "NyttPassord1", null, null), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        (await _harness.UserManager.HasPasswordAsync(user)).ShouldBeTrue();

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<PasswordChangedEvent>(e => e.UserId == user.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserAlreadyHasPassword_IsRejected()
    {
        var user = await _harness.CreateUserAsync("harallerede@test.local");
        var handler = CreateHandler();

        var result = await handler.Handle(
            new SetPasswordCommand(user.Id, "NyttPassord1", null, null), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_UnknownUser_Fails()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(
            new SetPasswordCommand(Guid.NewGuid(), "NyttPassord1", null, null), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    public void Dispose() => _harness.Dispose();
}
