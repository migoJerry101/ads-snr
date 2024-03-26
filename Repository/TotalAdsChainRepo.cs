using ads.Data;
using ads.Interface;
using ads.Models.Data;
using Microsoft.EntityFrameworkCore;
using static System.Reflection.Metadata.BlobBuilder;
using System.Data;
using System.Data.SqlClient;
using ads.Models.Dto.AdsChain;
using ads.Models.Dto.AdsClub;
using ads.Models.Dto.Sale;

namespace ads.Repository
{
    public class TotalAdsChainRepo : ITotalAdsChain
    {
        private readonly AdsContext _context;
        private readonly ILogs _logs;
        private readonly IConfiguration _configuration;
        private readonly ISales _sales;
        private readonly IInventory _inventory;

        public TotalAdsChainRepo(AdsContext context, ILogs logs, IConfiguration configuration, ISales sales, IInventory inventory)
        {
            _context = context;
            _logs = logs;
            _configuration = configuration;
            _sales = sales;
            _inventory = inventory;
        }
        public TotalAdsChain GetTotalAdsChain()
        {
            var totalAdsChain = _context.TotalAdsChains.FirstOrDefault();

            return totalAdsChain;
        }

        public async Task<List<AdsChainCreateDto>> GetTotalAdsChainByDate(string date)
        {
            var ads = await _context.TotalAdsChains
                .AsNoTracking()
                .Where(x => x.StartDate == date)
                .Select(a => new AdsChainCreateDto()
                {
                    Sales = a.Sales,
                    Ads = a.Ads,
                    Divisor = a.Divisor,
                    EndDate = a.EndDate,
                    Sku = a.Sku,
                    StartDate =a.StartDate
                })
                .OrderBy(y => y.EndDate)
                .ToListAsync();

            return ads;
        }

        public async Task DeleteAdsChainAsync(string date)
        {
            DateTime startLogs = DateTime.Now;
            List<Logging> Log = new List<Logging>();

            try
            {
                var strConn = _configuration["ConnectionStrings:DatabaseConnection"];
                //string strConn = "data source='199.84.0.201';Initial Catalog=ADS.UAT;User Id=sa;password=@dm1n@8800;Trusted_Connection=false;MultipleActiveResultSets=true;TrustServerCertificate=True;";
                var con = new SqlConnection(strConn);

                using (var command = new SqlCommand("_sp_DeleteAdsChainByDate", con))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@date", date);
                    command.CommandTimeout = 18000;
                    con.Open();

                    // Open the connection and execute the command
                    SqlDataReader reader = command.ExecuteReader();

                    reader.Close();
                    con.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);

                DateTime endLogs = DateTime.Now;

                Log.Add(new Logging
                {
                    StartLog = startLogs,
                    EndLog = endLogs,
                    Action = "Error",
                    Message = "Delete Ads Chain : " + e.Message + " ",
                    Record_Date = DateTime.ParseExact(date, "yyyy/MM/dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture)
                });

                _logs.InsertLogs(Log);
            }
        }

        public async Task<IEnumerable<IGrouping<string, AdsChainReportDto>>> GenerateAdsChainReportDto(DateTime startDate, DateTime endDate, IEnumerable<int> skus)
        {
            var adsChainReportDtos = new List<AdsChainReportDto>();
            var startLogs = DateTime.Now;
            var log = new List<Logging>();
            var skusAsString = skus.Select(s => s.ToString()).ToList();

            try
            {
                for (DateTime currentDate = endDate; startDate >= currentDate; currentDate = currentDate.AddDays(1))
                {
                    var sales = await _sales.GetSalesByDateAndClub(currentDate);
                    var filteredSales = skus.Count() > 0
                        ? sales.Where(x => skusAsString.Contains(x.Sku)).ToList() 
                        : sales;

                    var inventories = await _inventory.GetInventoriesByDateAndClubs(currentDate);
                    var filteredInventories = skus.Count() > 0
                        ? inventories.Where(x => skusAsString.Contains(x.Sku)).ToList() 
                        : inventories;

                    var salesDictionary = _sales.GetDictionayOfTotalSales(filteredSales);
                    var inventoriesDictionary = _inventory.GetDictionayOfTotalInventory(filteredInventories);

                    var adsChain = await _context.TotalAdsChains
                        .AsNoTracking()
                        .Where(x => x.StartDate == $"{currentDate:yyyy-MM-dd HH:mm:ss.fff}")
                        .OrderBy(z => z.Sku)
                        .Select(y =>
                           new AdsChainReportDto
                           {
                               Divisor = y.Divisor,
                               Ads = y.Ads,
                               Date = currentDate.ToString("M/d/yyyy"),
                               Sku = y.Sku
                           })
                        .ToListAsync();

                    var filteredAdsChain = skus.Count() > 0
                        ? adsChain.Where(x => skusAsString.Contains(x.Sku)).ToList()
                        : adsChain;

                    foreach (var adsItem in filteredAdsChain)
                    {
                        var todayKey = new SalesKey()
                        {
                            Sku = adsItem.Sku,
                            Date = currentDate
                        };

                        salesDictionary.TryGetValue((adsItem.Sku), out var salesToday);
                        inventoriesDictionary.TryGetValue(todayKey, out var InventoryToday);

                        adsItem.Sales = salesToday;
                        adsItem.OnHand = InventoryToday;
                    }

                    adsChainReportDtos.AddRange(filteredAdsChain);
                }

                var groupedByData = adsChainReportDtos.GroupBy(x => x.Sku);

                return groupedByData;
            }
            catch (Exception error)
            {
                DateTime endLogs = DateTime.Now;

                log.Add(new Logging
                {
                    StartLog = startLogs,
                    EndLog = endLogs,
                    Action = "Error",
                    Message = "Delete Ads Clubs : " + error.Message + " ",
                    Record_Date = endLogs.Date
                });

                _logs.InsertLogs(log);
                throw;
            }
        }
    }
}
