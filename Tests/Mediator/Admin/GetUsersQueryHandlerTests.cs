using Application.Mediator.Admin.Queries.GetUsers;
using Shouldly;
using Tests.Support;

namespace Tests.Mediator.Admin;

public class GetUsersQueryHandlerTests : IDisposable
{
    private readonly HandlerTestHarness _harness = new();

    private GetUsersQueryHandler CreateHandler() => new(_harness.UserManager);

    [Fact]
    public async Task Handle_ReturnsAllUsersOrderedByNewestFirst()
    {
        var first = await _harness.CreateUserAsync("forst@test.local");
        first.CreatedAt = DateTime.UtcNow.AddDays(-2);
        await _harness.DbContext.SaveChangesAsync();

        var second = await _harness.CreateUserAsync("sist@test.local");
        second.CreatedAt = DateTime.UtcNow;
        await _harness.DbContext.SaveChangesAsync();

        var handler = CreateHandler();
        var result = await handler.Handle(new GetUsersQuery(), CancellationToken.None);

        result.Count.ShouldBe(2);
        result[0].Email.ShouldBe("sist@test.local");
        result[1].Email.ShouldBe("forst@test.local");
    }

    [Fact]
    public async Task Handle_NoUsers_ReturnsEmptyList()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new GetUsersQuery(), CancellationToken.None);

        result.ShouldBeEmpty();
    }

    public void Dispose() => _harness.Dispose();
}
