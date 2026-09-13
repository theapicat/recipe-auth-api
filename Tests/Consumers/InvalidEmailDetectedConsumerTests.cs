using API.Consumers;
using Contracts.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OpenIddict.Abstractions;
using Shouldly;
using Tests.Support;

namespace Tests.Consumers;

public class InvalidEmailDetectedConsumerTests : IDisposable
{
    private readonly HandlerTestHarness _harness = new();
    private readonly IOpenIddictTokenManager _tokenManager = Substitute.For<IOpenIddictTokenManager>();
    private readonly ILogger<InvalidEmailDetectedConsumer> _logger = Substitute.For<ILogger<InvalidEmailDetectedConsumer>>();

    private InvalidEmailDetectedConsumer CreateConsumer() =>
        new(_harness.UserManager, _harness.DbContext, _tokenManager, _logger);

    private static ConsumeContext<InvalidEmailDetectedEvent> MockContext(InvalidEmailDetectedEvent message)
    {
        var context = Substitute.For<ConsumeContext<InvalidEmailDetectedEvent>>();
        context.Message.Returns(message);
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }

    [Fact]
    public async Task Consume_UserExistsWithActiveTokens_BlacklistsRevokesAndDeletes()
    {
        var user = await _harness.CreateUserAsync("bounced@test.local");

        // To "aktive tokens" som skal revokeres
        var token1 = new object();
        var token2 = new object();
        _tokenManager.FindBySubjectAsync(user.Id.ToString(), Arg.Any<CancellationToken>())
            .Returns(new List<object> { token1, token2 }.ToAsyncEnumerable());

        var consumer = CreateConsumer();
        var message = new InvalidEmailDetectedEvent
        {
            UserId = user.Id, Email = "bounced@test.local", Reason = "550 mailbox not found"
        };

        await consumer.Consume(MockContext(message));

        (await _harness.UserManager.FindByIdAsync(user.Id.ToString())).ShouldBeNull();

        var blacklisted = await _harness.DbContext.BlacklistedEntries
            .AnyAsync(b => b.Pattern == "bounced@test.local");
        blacklisted.ShouldBeTrue();

        await _tokenManager.Received(1).TryRevokeAsync(token1, Arg.Any<CancellationToken>());
        await _tokenManager.Received(1).TryRevokeAsync(token2, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_NoMatchingUser_OnlyBlacklistsEmail()
    {
        _tokenManager.FindBySubjectAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<object>().ToAsyncEnumerable());

        var consumer = CreateConsumer();
        var message = new InvalidEmailDetectedEvent
        {
            UserId = Guid.Empty, Email = "ukjent@test.local", Reason = "550 mailbox not found"
        };

        await consumer.Consume(MockContext(message));

        var blacklisted = await _harness.DbContext.BlacklistedEntries
            .AnyAsync(b => b.Pattern == "ukjent@test.local");
        blacklisted.ShouldBeTrue();
    }

    [Fact]
    public async Task Consume_UserLookupFallsBackToEmail_WhenUserIdIsEmpty()
    {
        var user = await _harness.CreateUserAsync("viaepost@test.local");
        _tokenManager.FindBySubjectAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<object>().ToAsyncEnumerable());

        var consumer = CreateConsumer();
        // UserId ikke oppgitt (Guid.Empty) - konsumenten skal falle tilbake til e-postoppslag
        var message = new InvalidEmailDetectedEvent
        {
            UserId = Guid.Empty, Email = "viaepost@test.local", Reason = "550 mailbox not found"
        };

        await consumer.Consume(MockContext(message));

        (await _harness.UserManager.FindByIdAsync(user.Id.ToString())).ShouldBeNull();
    }

    [Fact]
    public async Task Consume_SameEventDeliveredTwice_IsIdempotent()
    {
        _tokenManager.FindBySubjectAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<object>().ToAsyncEnumerable());

        var message = new InvalidEmailDetectedEvent
        {
            UserId = Guid.Empty, Email = "duplikat@test.local", Reason = "550 mailbox not found"
        };

        // Første levering
        await CreateConsumer().Consume(MockContext(message));

        // Andre levering av SAMME event (simulerer redelivery/at-least-once) - skal ikke kaste,
        // og skal ikke lage en ny duplikat svartelisteoppføring.
        await Should.NotThrowAsync(async () => await CreateConsumer().Consume(MockContext(message)));

        var count = await _harness.DbContext.BlacklistedEntries
            .CountAsync(b => b.Pattern == "duplikat@test.local");
        count.ShouldBe(1);
    }

    [Fact]
    public async Task Consume_EmailAlreadyBlacklisted_DoesNotDuplicateEntryButStillProcessesUser()
    {
        var user = await _harness.CreateUserAsync("alleredesvartelistet@test.local");
        _harness.DbContext.BlacklistedEntries.Add(new Domain.Entities.BlacklistedEntry
        {
            Pattern = "alleredesvartelistet@test.local",
            Type = Domain.Enums.BlacklistType.ExactEmail
        });
        await _harness.DbContext.SaveChangesAsync();

        _tokenManager.FindBySubjectAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<object>().ToAsyncEnumerable());

        var consumer = CreateConsumer();
        var message = new InvalidEmailDetectedEvent
        {
            UserId = user.Id, Email = "alleredesvartelistet@test.local", Reason = "550 mailbox not found"
        };

        await consumer.Consume(MockContext(message));

        var count = await _harness.DbContext.BlacklistedEntries
            .CountAsync(b => b.Pattern == "alleredesvartelistet@test.local");
        count.ShouldBe(1);
        (await _harness.UserManager.FindByIdAsync(user.Id.ToString())).ShouldBeNull();
    }

    public void Dispose() => _harness.Dispose();
}
