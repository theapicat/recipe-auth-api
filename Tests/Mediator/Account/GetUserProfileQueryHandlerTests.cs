using Application.Mediator.Account.Queries.GetUserProfile;
using Shouldly;
using Tests.Support;

namespace Tests.Mediator.Account;

public class GetUserProfileQueryHandlerTests : IDisposable
{
    private readonly HandlerTestHarness _harness = new();

    private GetUserProfileQueryHandler CreateHandler() => new(_harness.UserManager);

    [Fact]
    public async Task Handle_ExistingUser_ReturnsProfile()
    {
        var user = await _harness.CreateUserAsync("profil@test.local");
        var handler = CreateHandler();

        var result = await handler.Handle(new GetUserProfileQuery(user.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.UserProfile!.Email.ShouldBe("profil@test.local");
        result.UserProfile.HasPassword.ShouldBeTrue();
        result.UserProfile.IsGoogleAccount.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_UnknownUser_Fails()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new GetUserProfileQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    public void Dispose() => _harness.Dispose();
}
