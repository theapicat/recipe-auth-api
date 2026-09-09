using Contracts.Events;
using Domain.Entities;
using Domain.Enums;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using Persistence.Context;

namespace API.Consumers;

public class InvalidEmailDetectedConsumer(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    IOpenIddictTokenManager tokenManager,
    ILogger<InvalidEmailDetectedConsumer> logger) : IConsumer<InvalidEmailDetectedEvent>
{
    public async Task Consume(ConsumeContext<InvalidEmailDetectedEvent> context)
    {
        var message = context.Message;
        var normalizedEmail = message.Email.Trim().ToLowerInvariant();

        logger.LogWarning(
            "Hard bounce / ugyldig e-post registrert for {Email}. Årsak: {Reason}. Starter sletting og svartelisting.",
            normalizedEmail, message.Reason);

        // 1. Legg e-posten i svartelisten (dersom den ikke allerede finnes der)
        var isAlreadyBlacklisted = await dbContext.BlacklistedEntries
            .AnyAsync(b => b.Type == BlacklistType.ExactEmail && b.Pattern.ToLower() == normalizedEmail, context.CancellationToken);

        if (!isAlreadyBlacklisted)
        {
            var blacklistEntry = new BlacklistedEntry
            {
                Id = Guid.NewGuid(),
                Pattern = normalizedEmail,
                Type = BlacklistType.ExactEmail,
                Reason = $"Automatisk svartelistet pga. uleverbar e-postadresse ({message.Reason}).",
                CreatedAt = DateTime.UtcNow,
                CreatedByAdminId = Guid.Empty // Indikerer automatisk systemhandling
            };

            await dbContext.BlacklistedEntries.AddAsync(blacklistEntry, context.CancellationToken);
            await dbContext.SaveChangesAsync(context.CancellationToken);
        }

        // 2. Finn brukeren basert på UserId (hvis oppgitt) eller e-postadresse
        var user = (message.UserId != Guid.Empty ? await userManager.FindByIdAsync(message.UserId.ToString()) : null)
                   ?? await userManager.FindByEmailAsync(normalizedEmail);

        if (user is not null)
        {
            // 3. Inndra alle aktiverte OpenIddict-tokens (Refresh & Access tokens)
            var userTokens = tokenManager.FindBySubjectAsync(user.Id.ToString(), context.CancellationToken);
            await foreach (var token in userTokens)
            {
                await tokenManager.TryRevokeAsync(token, context.CancellationToken);
            }

            // 4. Slett brukeren fra Identity
            var deleteResult = await userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                logger.LogError("Kunne ikke slette bruker {UserId} ({Email}): {Errors}",
                    user.Id, user.Email, string.Join(", ", deleteResult.Errors.Select(e => e.Description)));
            }
            else
            {
                logger.LogInformation("Bruker {UserId} ({Email}) ble slettet fra databasen.", user.Id, user.Email);
            }
        }
        else
        {
            logger.LogInformation("Ingen aktiv brukerkonto funnet for {Email}, e-posten er kun lagt i svartelisten.", normalizedEmail);
        }
    }
}