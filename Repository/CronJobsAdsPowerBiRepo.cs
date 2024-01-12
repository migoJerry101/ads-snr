using ads.Interface;
using ads.Models.Data;
using ads.Utility;
using Quartz;

namespace ads.Repository
{
    public class CronJobsAdsPowerBiRepo : IJob
    {
        private readonly IPowerBiAds _powerBiAds;
        private readonly ILogs _logger;

        public CronJobsAdsPowerBiRepo(IPowerBiAds powerBiAds, ILogs logger)
        {
            _powerBiAds = powerBiAds;
            _logger = logger;
        }
        public  async Task Execute(IJobExecutionContext context)
        {
            var logs = new List<Logging>();
            var date = DateTime.Now;
            var adsDate = date.AddDays(-1);

            try
            {
                await _powerBiAds.ComputePowerBiAdsAsync(adsDate);

                //run tagging logic for report
                //generate report?
            }
            catch (Exception error)
            {
                DateTime endLogs = DateTime.Now;
                logs.Add(new Logging
                {
                    StartLog = date,
                    EndLog = endLogs,
                    Action = "Error",
                    Message = @$"Execute CronJobsAdsPowerBi : {error.Message}",
                    Record_Date = adsDate
                });

                _logger.InsertLogs(logs);

                throw;
            }
        }
    }
}
