using Application.Mediator.Admin.Commands.DeleteAndBlacklistUser;
using Contracts.Events.AdminActions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using Tests.Support;

namespace Tests.Mediator.Admin;

public class DeleteAndBlacklistUserCommandHandlerTests : IDisposable
{
    private readonly HandlerTestHarness _harness = new();
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();

    private DeleteAndBlacklistUserCommandHandler CreateHandler() =>
        new(_harness.UserManager, _harness.DbContext, _publishEndpoint);

    [Fact]
    public async Task Handle_DeletingRegularUser_BlacklistsAndDeletes()
    {
        var admin = await _harness.CreateUserAsync("admin@test.local", isAdmin: true);
        var target = await _harness.CreateUserAsync("baduser@test.local");
        var handler = CreateHandler();

        var result = await handler.Handle(
            new DeleteAndBlacklistUserCommand(target.Id.ToString(), "Brudd på vilkår", admin.Id),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        (await _harness.UserManager.FindByIdAsync(target.Id.ToString())).ShouldBeNull();

        var blacklisted = await _harness.DbContext.BlacklistedEntries
            .AnyAsync(b => b.Pattern == "baduser@test.local");
        blacklisted.ShouldBeTrue();

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<UserDeletedAndBlacklistedByAdminEvent>(e => e.UserId == target.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AdminTargetsOwnAccount_IsRejected()
    {
        var admin = await _harness.CreateUserAsync("admin@test.local", isAdmin: true);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new DeleteAndBlacklistUserCommand(admin.Id.ToString(), null, admin.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.IsBadRequest.ShouldBeTrue();
        (await _harness.UserManager.FindByIdAsync(admin.Id.ToString())).ShouldNotBeNull();
    }

    [Fact]
    public async Task Handle_AdminTargetsAnotherAdmin_IsRejected()
    {
        var actingAdmin = await _harness.CreateUserAsync("admin1@test.local", isAdmin: true);
        var otherAdmin = await _harness.CreateUserAsync("admin2@test.local", isAdmin: true);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new DeleteAndBlacklistUserCommand(otherAdmin.Id.ToString(), null, actingAdmin.Id),
            CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.IsBadRequest.ShouldBeTrue();
        (await _harness.UserManager.FindByIdAsync(otherAdmin.Id.ToString())).ShouldNotBeNull();

        var blacklisted = await _harness.DbContext.BlacklistedEntries
            .AnyAsync(b => b.Pattern == "admin2@test.local");
        blacklisted.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_UnknownUserId_ReturnsNotFound()
    {
        var admin = await _harness.CreateUserAsync("admin@test.local", isAdmin: true);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new DeleteAndBlacklistUserCommand(Guid.NewGuid().ToString(), null, admin.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.IsNotFound.ShouldBeTrue();
    }

    /// <summary>
    /// Regresjonstest for transaksjonen lagt til i DeleteAndBlacklistUserCommandHandler:
    /// dersom selve slettingen feiler, skal svartelisteoppføringen IKKE bli hengende igjen alene.
    ///
    /// Fremtvinger en deterministisk feil i UserManager.DeleteAsync ved å endre brukerens
    /// ConcurrencyStamp direkte i databasen (forbi EF Core sin change tracker) etter at
    /// brukeren allerede er sporet av DbContext-instansen handleren bruker. Handlerens egen
    /// FindByIdAsync-kall returnerer da den sporede (nå utdaterte) instansen via EF sin
    /// identity-resolution, og DeleteAsync sin optimistiske samtidighetssjekk feiler garantert
    /// (0 rader påvirket -> DbUpdateConcurrencyException -> IdentityResult.Failed).
    /// </summary>
    [Fact]
    public async Task Handle_WhenDeleteFailsDueToConcurrencyConflict_DoesNotLeaveOrphanedBlacklistEntry()
    {
        var admin = await _harness.CreateUserAsync("admin@test.local", isAdmin: true);
        var target = await _harness.CreateUserAsync("orphantest@test.local");

        // Spor brukeren i DbContext-instansen handleren skal gjenbruke.
        await _harness.UserManager.FindByIdAsync(target.Id.ToString());

        // Bump ConcurrencyStamp forbi change tracker slik at handlerens senere DeleteAsync
        // opererer på en utdatert stamp og garantert feiler.
        await _harness.DbContext.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE AspNetUsers SET ConcurrencyStamp = {Guid.NewGuid().ToString()} WHERE Id = {target.Id}");

        var handler = CreateHandler();
        var result = await handler.Handle(
            new DeleteAndBlacklistUserCommand(target.Id.ToString(), "test", admin.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();

        var blacklisted = await _harness.DbContext.BlacklistedEntries
            .AnyAsync(b => b.Pattern == "orphantest@test.local");
        blacklisted.ShouldBeFalse("svartelisteoppføringen skal rulles tilbake sammen med den mislykkede slettingen");
    }

    public void Dispose() => _harness.Dispose();
}
