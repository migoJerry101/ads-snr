using ads.Data;
using ads.Interface;
using ads.Models.Data;
using ads.Models.Dto.Condtx;
using Microsoft.Data.SqlClient;
using static System.Reflection.Metadata.BlobBuilder;

namespace ads.Repository
{
    public class CondtxRepo : ICondtx
    {
        private readonly IInventory _inventory;
        private readonly ILogs _log;
        public CondtxRepo(IInventory inventory, ILogs log)
        {
            _inventory = inventory;
            _log = log;
        }
        public async Task<IEnumerable<CondtxDto>> FetchTotalSalesFromMmsByDateAsync(DateTime dateTime)
        {
            var startLogs = DateTime.Now;
            var logs = new List<Logging>();
            var date = dateTime.ToString("yyMMdd");
            try
            {
                var totalSalesList = new List<CondtxDto>();

                using (var db = new OledbCon())
                {
                    await db.OpenAsync();
                    var query = $@"SELECT * FROM OPENQUERY([snr],'SELECT CSSKU,
                            CSSTOR,
                            CSEXPR
                          FROM MMJDALIB.CONDTX 
                          WHERE CSDATE = {date} 
                          AND CSSKU > 0')";

                    using var cmd = new SqlCommand(query, db.Con);

                    cmd.CommandTimeout = 18000;

                    using var reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        var sku = reader["CSSKU"].ToString();
                        var club = reader["CSSTOR"].ToString();
                        var totalSales = reader["CSEXPR"].ToString();

                        var price = new CondtxDto()
                        {
                            Sku = sku,
                            Club = club,
                            Value = decimal.TryParse(totalSales, out var valueOut) ? valueOut : 0,
                        };

                        totalSalesList.Add(price);
                    }
                }

                return totalSalesList;
            }
            catch (Exception error)
            {
                var endLogs = DateTime.Now;

                logs.Add(new Logging
                {
                    StartLog = startLogs,
                    EndLog = endLogs,
                    Action = "FetchTotalSalesFromMmsByDateAsync",
                    Message = error.Message,
                    Record_Date = startLogs.Date
                });

                _log.InsertLogs(logs);
                throw;
            }
        }

        public Dictionary<CondtxKey, decimal> GetTotalSalesDictionary(IEnumerable<CondtxDto> data)
        {
            var groupedByList = data
                .GroupBy(x => new CondtxKey(){ Sku = x.Sku, Club = x.Club })
                .ToDictionary(x => x.Key,
                group =>
                {
                    var count = group.Count();

                    if (count > 1)
                    {
                        return group.Where(x => x.Value >= 0).Sum(item => item.Value);
                    }

                    return group.Sum(item => item.Value);
                });

            return groupedByList;
        }
    }
}
