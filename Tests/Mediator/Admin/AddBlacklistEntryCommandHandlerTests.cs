using Application.Mediator.Admin.Commands.AddBlacklistEntry;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Tests.Support;

namespace Tests.Mediator.Admin;

public class AddBlacklistEntryCommandHandlerTests : IDisposable
{
    private readonly HandlerTestHarness _harness = new();

    private AddBlacklistEntryCommandHandler CreateHandler() => new(_harness.DbContext);

    [Fact]
    public async Task Handle_NewPattern_Succeeds()
    {
        var admin = await _harness.CreateUserAsync("admin@test.local", isAdmin: true);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new AddBlacklistEntryCommand("spam.test", BlacklistType.Domain, "Kjent spam-domene", admin.Id),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        (await _harness.DbContext.BlacklistedEntries.AnyAsync(b => b.Pattern == "spam.test")).ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_PatternIsNormalized_TrimmedLowercasedAndStrippedOfLeadingAt()
    {
        var admin = await _harness.CreateUserAsync("admin@test.local", isAdmin: true);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new AddBlacklistEntryCommand("  @SPAM.TEST  ", BlacklistType.Domain, null, admin.Id),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        (await _harness.DbContext.BlacklistedEntries.AnyAsync(b => b.Pattern == "spam.test")).ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_DuplicatePatternAndType_IsRejected()
    {
        var admin = await _harness.CreateUserAsync("admin@test.local", isAdmin: true);
        var handler = CreateHandler();

        await handler.Handle(new AddBlacklistEntryCommand("spam.test", BlacklistType.Domain, null, admin.Id),
            CancellationToken.None);
        var result = await handler.Handle(
            new AddBlacklistEntryCommand("spam.test", BlacklistType.Domain, null, admin.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        (await _harness.DbContext.BlacklistedEntries.CountAsync(b => b.Pattern == "spam.test")).ShouldBe(1);
    }

    [Fact]
    public async Task Handle_EmptyPattern_IsRejected()
    {
        var admin = await _harness.CreateUserAsync("admin@test.local", isAdmin: true);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new AddBlacklistEntryCommand("   ", BlacklistType.ExactEmail, null, admin.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    public void Dispose() => _harness.Dispose();
}
