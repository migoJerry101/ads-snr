using ads.Repository;
using Quartz;

namespace ads.Quarts
{
    public class ScheduleConfigurator
    {
        public static void ConfigureScheduleAds(IServiceCollectionQuartzConfigurator options)
        {
            options.UseMicrosoftDependencyInjectionJobFactory();

            var jobKey = new JobKey("DemoJob");
            options.AddJob<CronJobsADSRepo>(opts => opts.WithIdentity(jobKey));

            options.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("DemoJob-trigger")
                .WithCronSchedule("0 0 3 * * ?"));
        }

        public static void ConfigureScheduleAdsPowerBi(IServiceCollectionQuartzConfigurator options)
        {
            options.UseMicrosoftDependencyInjectionJobFactory();

            var jobKey = new JobKey("AnotherJob");
            options.AddJob<CronJobsPowerBi>(opts => opts.WithIdentity(jobKey));

            options.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("AnotherJob-trigger")
                .WithCronSchedule("0 0 4 * * ?"));
        }
    }
}
