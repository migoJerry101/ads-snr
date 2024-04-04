using ads.Data;
using ads.Interface;
using ads.Models.Data;
using Microsoft.EntityFrameworkCore;
using static System.Reflection.Metadata.BlobBuilder;
using System.Data;
using System.Data.SqlClient;
using ads.Models.Dto.AdsChain;
using ads.Models.Dto.AdsClub;
using DocumentFormat.OpenXml.InkML;
using ads.Models.Dto.Price;
using ads.Utility;
using ads.Models.Dto.Condtx;

namespace ads.Repository
{
    public class TotalAdsClubRepo : ITotalAdsClub
    {
        private readonly AdsContext _context;
        private readonly ILogs _logs;
        private readonly IConfiguration _configuration;
        private readonly ISales _sales;
        private readonly IInventory _inventory;
        private readonly IPrice _price;
        private readonly ICondtx _condtx;

        public TotalAdsClubRepo(
            AdsContext context,
            ILogs logs,
            IConfiguration configuration,
            ISales sales,
            IInventory inventory,
            IPrice price,
            ICondtx condtx)
        {
            _context = context;
            _logs = logs;
            _configuration = configuration;
            _sales = sales;
            _inventory = inventory;
            _price = price;
            _condtx = condtx;
        }

        public async Task<(List<TotalAdsClub>, int totalPages)> GetPaginatedTotalAdsClubs(TotalAdsChainPaginationDto data)
        {
            var ads = _context.TotalAdsClubs.Where(x =>
                    (string.IsNullOrEmpty(data.Club) || x.Clubs == data.Club) &&
                    (string.IsNullOrEmpty(data.Sku) || x.Sku == data.Sku) &&
                    x.StartDate == data.StartDate);

            var adsCount = await ads.CountAsync();
            var totalPages = (int)Math.Ceiling((double)adsCount / data.PageSize);

            var paginatedAds = await ads
                .Skip((data.PageNumber - 1) * data.PageSize)
                .Take(data.PageSize)
                .ToListAsync();

            return (paginatedAds, totalPages);
        }

        public async Task<List<AdsClubCreateDto>> GetTotalAdsClubsByDate(string date)
        {
            var ads = await _context.TotalAdsClubs
                .AsNoTracking()
                .Where(x => x.StartDate == date)
                .Select(y => new AdsClubCreateDto()
                {
                    Ads = y.Ads,
                    Clubs = y.Clubs,
                    Divisor = y.Divisor,
                    StartDate = y.StartDate,
                    Sku = y.Sku,
                    EndDate = y.EndDate,
                    OverallSales = y.OverallSales ?? 0,
                    Sales = y.Sales
                })
                .OrderBy(y => y.EndDate)
                .ToListAsync();

            return ads;
        }

        public async Task DeleteAdsClubsAsync(string date)
        {
            DateTime startLogs = DateTime.Now;
            List<Logging> Log = new List<Logging>();

            try
            {
                var strConn = _configuration["ConnectionStrings:DatabaseConnection"];
                //string strConn = "data source='199.84.0.201';Initial Catalog=ADS.UAT;User Id=sa;password=@dm1n@8800;Trusted_Connection=false;MultipleActiveResultSets=true;TrustServerCertificate=True;";
                var con = new SqlConnection(strConn);

                using (var command = new SqlCommand("_sp_DeleteAdsClubsByDate", con))
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
                    Message = "Delete Ads Clubs : " + e.Message + " ",
                    Record_Date = DateTime.ParseExact(date, "yyyy/MM/dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture)
                });

                _logs.InsertLogs(Log);
            }
        }

        public async Task<IEnumerable<IGrouping<string, AdsClubReportDto>>> GenerateAdsClubsReportDto(DateTime startDate, DateTime endDate, IEnumerable<int> skus)
        {
            var adsClubReportDtos = new List<AdsClubReportDto>();
            var startLogs = DateTime.Now;
            var log = new List<Logging>();
            var skusAsString = skus.Select(s => s.ToString());

            try
            {
                for (DateTime currentDate = endDate; startDate >= currentDate; currentDate = currentDate.AddDays(1))
                {
                    var sales =  await _sales.GetSalesByDateAndClub(currentDate);
                    var filteredSales = skus.Count() > 0
                        ? sales.Where(x => skusAsString.Contains(x.Sku)).ToList()
                        : sales;
                    var salesDictionary = filteredSales
                        .GroupBy(x => (x.Sku, x.Clubs))
                        .ToDictionary(group => group.Key, group => group.Sum(y => y.Sales));

                    var inventories = await _inventory.GetInventoriesByDateAndClubs(currentDate);
                    var filteredInventories = skus.Count() > 0
                        ? inventories.Where(x => skusAsString.Contains(x.Sku)).ToList()
                        : inventories;
                    var inventoriesDictionary = filteredInventories
                        .ToDictionary(x => (x.Sku, x.Clubs), y => y.Inventory);

                    var adsClubs = await _context.TotalAdsClubs
                        .Where(x => x.StartDate == $"{currentDate:yyyy-MM-dd HH:mm:ss.fff}")
                        .OrderBy(z => z.Clubs)
                        .OrderBy(a => a.Sku)
                        .Select(y =>
                           new AdsClubReportDto
                           {
                               Divisor = y.Divisor,
                               Ads = y.Ads,
                               Date = currentDate.ToString("M/d/yyyy"),
                               Clubs = y.Clubs,
                               Sku = y.Sku
                           })
                        .ToListAsync();

                    var filtered = skus.Count() > 0
                        ? adsClubs.Where(x => skusAsString.Contains(x.Sku)).ToList()
                        : adsClubs;

                    foreach (var adsItem in filtered)
                    {
                        salesDictionary.TryGetValue((adsItem.Sku, adsItem.Clubs), out var salesToday);
                        inventoriesDictionary.TryGetValue((adsItem.Sku, adsItem.Clubs), out var InventoryToday);

                        adsItem.Sales = salesToday;
                        adsItem.OnHand = InventoryToday;
                    }

                    adsClubReportDtos.AddRange(filtered);
                }

                var groupBy = adsClubReportDtos.GroupBy(x => x.Sku);

                return groupBy;
            }
            catch (Exception error)
            {
                var endLogs = DateTime.Now;

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

        public async Task UpdateClubTotalAverageSales(DateTime date)
        {
            var Log = new List<Logging>();
            DateTime startLogs = DateTime.Now;

            try
            {
                var price = await _price.GetPricesByDateAsync(date);
                var sales = await _sales.GetSalesByDateEf(date);
                var salesDictionary = sales.
                    GroupBy(x => new { x.Sku, x.Clubs })
                    .ToDictionary(
                         group => group.Key,
                         group =>
                         {
                             var count = group.Count();

                             if (count > 1)
                             {
                                 return group.Where(x => x.Sales >= 0).Sum(item => item.Sales);
                             }

                             return group.Sum(item => item.Sales);
                         }
                 );

                var priceDictionary = price.ToDictionary(x => new PriceKey() { Club = x.Club, Sku = x.Sku }, x => x);

                var adsClubs = await _context.TotalAdsClubs.Where(x => x.StartDate == $"{date:yyyy-MM-dd HH:mm:ss.fff}").ToListAsync();

                foreach (var adsClub in adsClubs)
                {
                    var hasPrice = priceDictionary
                        .TryGetValue(new PriceKey()
                        {
                            Club = adsClub.Clubs,
                            Sku = adsClub.Sku
                        }, out var priceOut);

                    var hasSales = salesDictionary.TryGetValue(new { adsClub.Sku, adsClub.Clubs }, out var salesOut);

                    if (hasSales && hasPrice && priceOut is not null) adsClub.OverallSales = priceOut.Value * salesOut;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception error)
            {
                DateTime endLogs = DateTime.Now;
                Log.Add(new Logging
                {
                    StartLog = startLogs,
                    EndLog = endLogs,
                    Action = "UpdateClubTotalAverageSales",
                    Message = error.Message,
                    Record_Date = date
                });

                _logs.InsertLogs(Log);
            }
        }

        public async Task<List<AdsClubCreateDto>> GetAdsClubs()
        {
            try
            {
                var newDate = DateTime.Now.AddDays(-1).Date;
                var test = $"{newDate:yyyy-MM-dd HH:mm:ss.fff}";
                var list2 = await _context.TotalAdsClubs
                    .Where(x => x.StartDate == $"{newDate:yyyy-MM-dd HH:mm:ss.fff}")
                    .AsNoTracking()
                    .ToListAsync();

                var list = await _context.TotalAdsClubs
                    .Where(x => x.StartDate == $"{newDate:yyyy-MM-dd HH:mm:ss.fff}")
                    .AsNoTracking()
                    .Select(y => new AdsClubCreateDto()
                    {
                        Ads = y.Ads,
                        Clubs = y.Clubs,
                        Divisor = y.Divisor,
                        StartDate = y.StartDate,
                        Sku = y.Sku,
                        EndDate = y.EndDate,
                        OverallSales = y.OverallSales ?? 0,
                        Sales = y.Sales
                    }).ToListAsync();

                return list;
            }
            catch (Exception error)
            {

                throw;
            }
        }

        public async Task UpdateClubOverallSalesByDateCondtx(DateTime date)
        {
            var Log = new List<Logging>();
            DateTime startLogs = DateTime.Now;

            try
            {
                var adsClubs = await _context.TotalAdsClubs
                    .Where(x => x.StartDate == $"{date:yyyy-MM-dd HH:mm:ss.fff}")
                    .ToListAsync();

                var condtxSales = await _condtx.FetchTotalSalesFromMmsByDateAsync(date);
                var salesDictionary = _condtx.GetTotalSalesDictionary(condtxSales);

                foreach (var ads in adsClubs)
                {
                    var key = new CondtxKey()
                    {
                        Club = ads.Clubs,
                        Sku = ads.Sku
                    };

                    if(salesDictionary.TryGetValue(key, out var totalSales)) ads.OverallSales = totalSales;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception error)
            {
                DateTime endLogs = DateTime.Now;
                Log.Add(new Logging
                {
                    StartLog = startLogs,
                    EndLog = endLogs,
                    Action = "UpdateClubTotalAverageSales",
                    Message = error.Message,
                    Record_Date = date
                }); ;

                _logs.InsertLogs(Log);
            }
        }
    }
}
