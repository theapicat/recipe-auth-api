using API.Jobs;
using Domain.Options;
using Quartz;

namespace API.Extensions;

public static class QuartzExtensions
{
    public static IServiceCollection AddQuartzJobs(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Registrer opsjoner for Dependency Injection (DI)
        var section = configuration.GetSection(AccountLifecycleOptions.SectionName);
        services.Configure<AccountLifecycleOptions>(section);

        // 2. Bind verdier for oppstartskonfigurasjon med moderne null-håndtering
        var options = section.Get<AccountLifecycleOptions>() ?? new AccountLifecycleOptions();

        // 3. Fallback/Validering hvis Cron-streng mangler (forhindrer krasj)
        var cronSchedule = string.IsNullOrWhiteSpace(options.CronSchedule) 
            ? "0 0 0 * * ?" // Standard: Hver natt kl 00:00
            : options.CronSchedule;

        services.AddQuartz(q =>
        {
            var jobKey = new JobKey(nameof(AccountLifecycleJob));

            q.AddJob<AccountLifecycleJob>(opts => opts.WithIdentity(jobKey));

            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity($"{nameof(AccountLifecycleJob)}-trigger")
                .WithCronSchedule(cronSchedule));
        });

        // 4. Sikre at bakgrunnstjenesten venter på at jobber fullføres ved shutdown
        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        return services;
    }
}