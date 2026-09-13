using Application.Mediator.Admin.Queries.GetBlacklistEntry;
using Domain.Entities;
using Domain.Enums;
using Shouldly;
using Tests.Support;

namespace Tests.Mediator.Admin;

public class GetBlacklistEntriesQueryHandlerTests : IDisposable
{
    private readonly HandlerTestHarness _harness = new();

    private GetBlacklistEntriesQueryHandler CreateHandler() => new(_harness.DbContext);

    [Fact]
    public async Task Handle_ReturnsAllEntriesOrderedByNewestFirst()
    {
        _harness.DbContext.BlacklistedEntries.Add(new BlacklistedEntry
        {
            Pattern = "gammel.test", Type = BlacklistType.Domain, CreatedAt = DateTime.UtcNow.AddDays(-5)
        });
        _harness.DbContext.BlacklistedEntries.Add(new BlacklistedEntry
        {
            Pattern = "ny.test", Type = BlacklistType.Domain, CreatedAt = DateTime.UtcNow
        });
        await _harness.DbContext.SaveChangesAsync();

        var handler = CreateHandler();
        var result = await handler.Handle(new GetBlacklistEntriesQuery(), CancellationToken.None);

        result.Count.ShouldBe(2);
        result[0].Pattern.ShouldBe("ny.test");
        result[1].Pattern.ShouldBe("gammel.test");
    }

    [Fact]
    public async Task Handle_NoEntries_ReturnsEmptyList()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new GetBlacklistEntriesQuery(), CancellationToken.None);

        result.ShouldBeEmpty();
    }

    public void Dispose() => _harness.Dispose();
}
