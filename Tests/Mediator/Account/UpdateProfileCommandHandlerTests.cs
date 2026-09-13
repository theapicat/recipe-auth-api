using Application.Mediator.Account.Commands.UpdateProfile;
using Shouldly;
using Tests.Support;

namespace Tests.Mediator.Account;

public class UpdateProfileCommandHandlerTests : IDisposable
{
    private readonly HandlerTestHarness _harness = new();

    private UpdateProfileCommandHandler CreateHandler() => new(_harness.UserManager);

    [Fact]
    public async Task Handle_RegularUser_UpdatesNameFields()
    {
        var user = await _harness.CreateUserAsync("bruker@test.local");
        var handler = CreateHandler();

        var result = await handler.Handle(new UpdateProfileCommand(user.Id, "Nytt", "Etternavn"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.UserProfile!.FirstName.ShouldBe("Nytt");

        var updated = await _harness.UserManager.FindByIdAsync(user.Id.ToString());
        updated!.FirstName.ShouldBe("Nytt");
        updated.LastName.ShouldBe("Etternavn");
    }

    /// <summary>
    /// Systemadministratorens profilinformasjon er låst mot selv-redigering via denne
    /// selvbetjenings-ruten (i motsetning til /api/auth/admin/users, som andre admins bruker).
    /// </summary>
    [Fact]
    public async Task Handle_AdminAccount_CannotEditOwnProfileViaSelfService()
    {
        var admin = await _harness.CreateUserAsync("admin@test.local", isAdmin: true);
        var handler = CreateHandler();

        var result = await handler.Handle(new UpdateProfileCommand(admin.Id, "Hacket", "Navn"), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.IsForbidden.ShouldBeTrue();

        var unchanged = await _harness.UserManager.FindByIdAsync(admin.Id.ToString());
        unchanged!.FirstName.ShouldBe("Test"); // uendret, satt av HandlerTestHarness.CreateUserAsync
    }

    [Fact]
    public async Task Handle_UnknownUser_Fails()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new UpdateProfileCommand(Guid.NewGuid(), "X", "Y"), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    public void Dispose() => _harness.Dispose();
}
