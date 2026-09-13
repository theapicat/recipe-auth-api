using Application.Mediator.Account.Commands.RemoveBlacklistEntry;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Tests.Support;

namespace Tests.Mediator.Account;

public class RemoveBlacklistEntryCommandHandlerTests : IDisposable
{
    private readonly HandlerTestHarness _harness = new();

    private RemoveBlacklistEntryCommandHandler CreateHandler() => new(_harness.DbContext);

    [Fact]
    public async Task Handle_ExistingEntry_RemovesItAndReturnsTrue()
    {
        var entry = new BlacklistedEntry { Pattern = "fjern@test.local", Type = BlacklistType.ExactEmail };
        _harness.DbContext.BlacklistedEntries.Add(entry);
        await _harness.DbContext.SaveChangesAsync();

        var handler = CreateHandler();
        var result = await handler.Handle(new RemoveBlacklistEntryCommand(entry.Id), CancellationToken.None);

        result.ShouldBeTrue();
        (await _harness.DbContext.BlacklistedEntries.AnyAsync(b => b.Id == entry.Id)).ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_UnknownEntry_ReturnsFalse()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new RemoveBlacklistEntryCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldBeFalse();
    }

    public void Dispose() => _harness.Dispose();
}
