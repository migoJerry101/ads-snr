using ads.Interface;
using ads.Models.Data;
using ads.Utility;
using Quartz;

namespace ads.Repository
{
    public class CronJobsPowerBi : IJob
    {
        private readonly ILogs _logs;

        public CronJobsPowerBi(ILogs logs)
        {
            _logs = logs;
        }

        public Task Execute(IJobExecutionContext context)
        {
            var startLogs = DateTime.Now;

            try
            {

            }
            catch (Exception error)
            {

                DateTime endLogs = DateTime.Now;
                var log = new List<Logging>();

                log.Add(new Logging
                {
                    StartLog = startLogs,
                    EndLog = endLogs,
                    Action = "Error",
                    Message = error.Message,
                    Record_Date = startLogs.Date
                });

                _logs.InsertLogs(log);

                throw;
            }
            throw new NotImplementedException();
        }
    }
}
