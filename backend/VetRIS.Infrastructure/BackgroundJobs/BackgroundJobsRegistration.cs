using Microsoft.Extensions.DependencyInjection;
using Quartz;
using VetRIS.Infrastructure.BackgroundJobs;

namespace VetRIS.Infrastructure;

public static class BackgroundJobsRegistration
{
    /// <summary>Registers Quartz.NET and every scheduled workflow-service job with its cron trigger. Called once from the Web API's Program.cs, alongside AddInfrastructure.</summary>
    public static IServiceCollection AddBackgroundJobs(this IServiceCollection services)
    {
        services.AddQuartz(q =>
        {
            AddJobWithCronTrigger<StudyArchiveJob>(q, StudyArchiveJob.CronSchedule);
            AddJobWithCronTrigger<DayEndJob>(q, DayEndJob.CronSchedule);
            AddJobWithCronTrigger<FileDistributionJob>(q, FileDistributionJob.CronSchedule);
            AddJobWithCronTrigger<StudyWritebackJob>(q, StudyWritebackJob.CronSchedule);
            AddJobWithCronTrigger<MonthlyBillingSummaryJob>(q, MonthlyBillingSummaryJob.CronSchedule);
        });

        // Waits for in-flight jobs to finish on shutdown rather than killing
        // them mid-write — matters here since every job ends with a
        // SaveChangesAsync.
        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

        return services;
    }

    private static void AddJobWithCronTrigger<TJob>(IServiceCollectionQuartzConfigurator quartz, string cronExpression) where TJob : IJob
    {
        var jobKey = new JobKey(typeof(TJob).Name);

        quartz.AddJob<TJob>(opts => opts.WithIdentity(jobKey));
        quartz.AddTrigger(opts => opts
            .ForJob(jobKey)
            .WithIdentity($"{typeof(TJob).Name}-trigger")
            .WithCronSchedule(cronExpression));
    }
}
