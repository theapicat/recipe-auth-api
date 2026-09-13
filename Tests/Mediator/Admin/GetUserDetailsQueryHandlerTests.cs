using Application.Mediator.Admin.Queries.GetUserDetails;
using Shouldly;
using Tests.Support;

namespace Tests.Mediator.Admin;

public class GetUserDetailsQueryHandlerTests : IDisposable
{
    private readonly HandlerTestHarness _harness = new();

    private GetUserDetailsQueryHandler CreateHandler() => new(_harness.UserManager);

    [Fact]
    public async Task Handle_ExistingUser_ReturnsFullDetails()
    {
        var user = await _harness.CreateUserAsync("detaljer@test.local");
        var handler = CreateHandler();

        var result = await handler.Handle(new GetUserDetailsQuery(user.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Details!.Email.ShouldBe("detaljer@test.local");
        result.Details.Role.ShouldBe("User");
        result.Details.HasPassword.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_UnknownUser_ReturnsNotFound()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new GetUserDetailsQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    public void Dispose() => _harness.Dispose();
}
