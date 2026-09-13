using API.Jobs;
using Contracts.Events.SystemActions;
using Domain.Enums;
using Domain.Options;
using MassTransit;
using Microsoft.Extensions.Options;
using NSubstitute;
using Persistence.Context;
using Quartz;
using Shouldly;
using Tests.Support;

namespace Tests.Jobs;

public class AccountLifecycleJobTests : IDisposable
{
    private readonly HandlerTestHarness _harness = new();
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();
    private readonly IOptions<AccountLifecycleOptions> _options = Options.Create(new AccountLifecycleOptions());
    private readonly IOptions<AppSettings> _appSettings = Options.Create(new AppSettings { FrontendUrl = "http://localhost:3000" });
    private readonly IJobExecutionContext _context = Substitute.For<IJobExecutionContext>();

    private AccountLifecycleJob CreateJob() =>
        new(_harness.UserManager, _harness.DbContext, _publishEndpoint, _options, _appSettings);

    private async Task SetCreatedAtAsync(ApplicationUser user, DateTime createdAt)
    {
        user.CreatedAt = createdAt;
        await _harness.DbContext.SaveChangesAsync();
    }

    private async Task SetLastLoginAtAsync(ApplicationUser user, DateTime? lastLoginAt)
    {
        user.LastLoginAt = lastLoginAt;
        await _harness.DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Execute_UnconfirmedUserOlderThan7Days_GetsReminded()
    {
        var user = await _harness.CreateUserAsync("uke1@test.local", emailConfirmed: false);
        await SetCreatedAtAsync(user, DateTime.UtcNow.AddDays(-8));

        await CreateJob().Execute(_context);

        var updated = await _harness.UserManager.FindByIdAsync(user.Id.ToString());
        updated.ShouldNotBeNull("brukeren skal ikke slettes eller sperres på dette stadiet");
        updated!.Confirmation7DaysReminderSentAt.ShouldNotBeNull();
        (await _harness.UserManager.IsLockedOutAsync(updated)).ShouldBeFalse();

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<Confirmation7DaysReminderEvent>(e => e.UserId == user.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_UnconfirmedUserOlderThan14Days_GetsLocked()
    {
        var user = await _harness.CreateUserAsync("uke2@test.local", emailConfirmed: false);
        await SetCreatedAtAsync(user, DateTime.UtcNow.AddDays(-15));

        await CreateJob().Execute(_context);

        var updated = await _harness.UserManager.FindByIdAsync(user.Id.ToString());
        updated.ShouldNotBeNull();
        (await _harness.UserManager.IsLockedOutAsync(updated!)).ShouldBeTrue();
        updated!.LockoutReason.ShouldBe(LockoutReason.UnconfirmedEmail14Days);

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<Confirmation14DaysReminderEvent>(e => e.UserId == user.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_UnconfirmedUserOlderThan30Days_GetsDeleted()
    {
        var user = await _harness.CreateUserAsync("uke3@test.local", emailConfirmed: false);
        await SetCreatedAtAsync(user, DateTime.UtcNow.AddDays(-31));

        await CreateJob().Execute(_context);

        (await _harness.UserManager.FindByIdAsync(user.Id.ToString())).ShouldBeNull();

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<UserAccountDeletedBySystemEvent>(e => e.UserId == user.Id), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dokumenterer eksplisitt overlapp-scenarioet fra Documentation/06-test-strategy.md pkt. 4.4:
    /// de seks skannene ekskluderer ikke hverandre, så en konto som ikke er behandlet på lenge
    /// (f.eks. etter nedetid på jobben) kan bli påminnet, sperret OG slettet i samme kjøring.
    /// Sluttresultatet (sletting) er korrekt uansett - dette er ikke en bug, men bør være
    /// eksplisitt dokumentert atferd, ikke noe som oppdages ved en tilfeldighet.
    /// </summary>
    [Fact]
    public async Task Execute_UnconfirmedUserOlderThan30DaysOnFirstEverRun_TriggersAllThreeStagesInOneExecution()
    {
        var user = await _harness.CreateUserAsync("langnedetid@test.local", emailConfirmed: false);
        await SetCreatedAtAsync(user, DateTime.UtcNow.AddDays(-35));

        await CreateJob().Execute(_context);

        // Sluttresultat: brukeren er slettet
        (await _harness.UserManager.FindByIdAsync(user.Id.ToString())).ShouldBeNull();

        // Men alle tre hendelsene ble publisert i SAMME jobbkjøring
        await _publishEndpoint.Received(1).Publish(
            Arg.Is<Confirmation7DaysReminderEvent>(e => e.UserId == user.Id), Arg.Any<CancellationToken>());
        await _publishEndpoint.Received(1).Publish(
            Arg.Is<Confirmation14DaysReminderEvent>(e => e.UserId == user.Id), Arg.Any<CancellationToken>());
        await _publishEndpoint.Received(1).Publish(
            Arg.Is<UserAccountDeletedBySystemEvent>(e => e.UserId == user.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_ConfirmedUserInactiveOver6Months_GetsWarned()
    {
        var user = await _harness.CreateUserAsync("inaktiv6mnd@test.local");
        await SetLastLoginAtAsync(user, DateTime.UtcNow.AddMonths(-7));

        await CreateJob().Execute(_context);

        var updated = await _harness.UserManager.FindByIdAsync(user.Id.ToString());
        updated.ShouldNotBeNull();
        updated!.InactivityWarning6MonthsSentAt.ShouldNotBeNull();
        (await _harness.UserManager.IsLockedOutAsync(updated)).ShouldBeFalse();

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<Inactivity6MonthsWarningEvent>(e => e.UserId == user.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_ConfirmedUserInactiveOver1Year_GetsLocked()
    {
        var user = await _harness.CreateUserAsync("inaktiv1ar@test.local");
        await SetLastLoginAtAsync(user, DateTime.UtcNow.AddMonths(-13));

        await CreateJob().Execute(_context);

        var updated = await _harness.UserManager.FindByIdAsync(user.Id.ToString());
        updated.ShouldNotBeNull();
        (await _harness.UserManager.IsLockedOutAsync(updated!)).ShouldBeTrue();
        updated!.LockoutReason.ShouldBe(LockoutReason.Inactivity1Year);

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<Inactivity1YearLockedEvent>(e => e.UserId == user.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_UserLockedForInactivityOver30DaysWithoutReactivation_GetsDeleted()
    {
        var user = await _harness.CreateUserAsync("inaktivsletting@test.local");
        // Preset Inactivity1YearLockedSentAt slik at kun skann 6 (sletting) sin spesifikke
        // betingelse isoleres og testes - skann 5 hopper automatisk over siden den krever at
        // dette feltet er null.
        await SetLastLoginAtAsync(user, DateTime.UtcNow.AddMonths(-14));
        user.Inactivity1YearLockedSentAt = DateTime.UtcNow.AddDays(-31);
        await _harness.DbContext.SaveChangesAsync();

        await CreateJob().Execute(_context);

        (await _harness.UserManager.FindByIdAsync(user.Id.ToString())).ShouldBeNull();

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<UserAccountDeletedBySystemEvent>(e => e.UserId == user.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_AdminAccountMeetingAllCriteria_IsNeverTouchedByAnyScan()
    {
        var admin = await _harness.CreateUserAsync("admin@test.local", isAdmin: true, emailConfirmed: false);
        await SetCreatedAtAsync(admin, DateTime.UtcNow.AddDays(-400));
        await SetLastLoginAtAsync(admin, DateTime.UtcNow.AddYears(-2));

        await CreateJob().Execute(_context);

        var stillThere = await _harness.UserManager.FindByIdAsync(admin.Id.ToString());
        stillThere.ShouldNotBeNull("admin-kontoer skal aldri røres av AccountLifecycleJob");
        (await _harness.UserManager.IsLockedOutAsync(stillThere!)).ShouldBeFalse();
        stillThere!.Confirmation7DaysReminderSentAt.ShouldBeNull();
        stillThere.Confirmation14DaysLockedSentAt.ShouldBeNull();
        stillThere.InactivityWarning6MonthsSentAt.ShouldBeNull();
        stillThere.Inactivity1YearLockedSentAt.ShouldBeNull();

        await _publishEndpoint.DidNotReceive().Publish(Arg.Any<Confirmation7DaysReminderEvent>(), Arg.Any<CancellationToken>());
        await _publishEndpoint.DidNotReceive().Publish(Arg.Any<Confirmation14DaysReminderEvent>(), Arg.Any<CancellationToken>());
        await _publishEndpoint.DidNotReceive().Publish(Arg.Any<UserAccountDeletedBySystemEvent>(), Arg.Any<CancellationToken>());
    }

    public void Dispose() => _harness.Dispose();
}
