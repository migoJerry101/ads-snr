using ads.Data;
using ads.Interface;
using ads.Models.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ads.Repository
{
    public class PowerBiAdsChainRepo : IPowerBiAdsChain
    {
        private readonly AdsContex _adsContex;
        private readonly ILogs _logger;

        public PowerBiAdsChainRepo(AdsContex adsContex, ILogs logger)
        {
            _adsContex = adsContex;
            _logger = logger;
        }
        public async Task<List<PowerBiAdsChain>> GetPowerBiAdsChainsByDateAsync(DateTime date)
        {
            var log = new List<Logging>();
            var startLogs = DateTime.Now;

            try
            {
                var ads = await _adsContex.PowerBiAdsChains.Where(x => x.StartDate == date).ToListAsync();

                return ads;
            }
            catch (Exception error)
            {
                var endLogs = DateTime.Now;
                log.Add(new Logging
                {
                    StartLog = startLogs,
                    EndLog = endLogs,
                    Action = "Tags for Ads Clubs",
                    Message = $"Error: {error.Message}",
                    Record_Date = date.Date
                });

                _logger.InsertLogs(log);

                throw;
            }
        }

        public async Task SavePowerBiChainAsync(List<PowerBiAdsChain> ads, DateTime date)
        {
            var log = new List<Logging>();

            try
            {
                using(var db = new OledbCon())
                {
                    await db.OpenAsync();

                    using (var transaction = db.Con.BeginTransaction())
                    {
                        using (var bulkCopy = new SqlBulkCopy(db.Con, SqlBulkCopyOptions.Default, transaction))
                        {
                            bulkCopy.DestinationTableName = "tbl_powerBiAdsChains";
                            bulkCopy.BatchSize = 1000;

                            var dataTable = new DataTable();
                            dataTable.Columns.Add("Id", typeof(int));
                            dataTable.Columns.Add("Sku", typeof(string));
                            dataTable.Columns.Add("Sales", typeof(decimal));
                            dataTable.Columns.Add("Divisor", typeof(int));
                            dataTable.Columns.Add("Ads", typeof(decimal));
                            dataTable.Columns.Add("StartDate", typeof(string));
                            dataTable.Columns.Add("EndDate", typeof(string));
                            dataTable.Columns.Add("OutOfStockDaysCount", typeof(int));

                            foreach (var rawData in ads)
                            {
                                var row = dataTable.NewRow();
                                row["Sku"] = rawData.Sku;
                                row["Sales"] = rawData.Sales;
                                row["Divisor"] = rawData.Divisor;
                                row["Ads"] = rawData.Ads;
                                row["StartDate"] = rawData.StartDate;
                                row["EndDate"] = rawData.EndDate;
                                row["OutOfStockDaysCount"] = rawData.OutOfStockDaysCount;

                                dataTable.Rows.Add(row);
                            }

                            await bulkCopy.WriteToServerAsync(dataTable);
                        }

                        transaction.Commit();
                    }

                    DateTime endLogs = DateTime.Now;
                    log.Add(new Logging
                    {
                        StartLog = date,
                        EndLog = endLogs,
                        Action = "Total ADS",
                        Message = $"Total Clubs Inserted : {ads.Count()}",
                        Record_Date = endLogs.AddDays(-1).Date
                    });

                    _logger.InsertLogs(log);
                }
            }
            catch (Exception error)
            {

                var endLogs = DateTime.Now;
                log.Add(new Logging
                {
                    StartLog = date,
                    EndLog = endLogs,
                    Action = "Tags for Ads Clubs",
                    Message = $"Error: {error.Message}",
                    Record_Date = date.Date
                });

                _logger.InsertLogs(log);
            }
        }
    }
}
