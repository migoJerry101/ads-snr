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

namespace ads.Repository
{
    public class TotalAdsClubRepo : ITotalAdsClub
    {
        private readonly AdsContext _context;
        private readonly ILogs _logs;
        private readonly IConfiguration _configuration;
        private readonly ISales _sales;
        private readonly IInventory _inventory;

        public TotalAdsClubRepo(
            AdsContext context,
            ILogs logs,
            IConfiguration configuration,
            ISales sales,
            IInventory inventory)
        {
            _context = context;
            _logs = logs;
            _configuration = configuration;
            _sales = sales;
            _inventory = inventory;
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

        public async Task<List<TotalAdsClub>> GetTotalAdsClubsByDate(string date)
        {
            var ads = await _context.TotalAdsClubs
                .Where(x => x.StartDate == date)
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

        public async Task<IEnumerable<IGrouping<string, AdsClubReportDto>>> GenerateAdsClubsReportDto(DateTime startDate, DateTime endDate)
        {
            var adsClubReportDtos = new List<AdsClubReportDto>();
            var startLogs = DateTime.Now;
            var log = new List<Logging>();

            try
            {
                for (DateTime currentDate = endDate; startDate >= currentDate; currentDate = currentDate.AddDays(1))
                {
                    var sales = await _sales.GetSalesByDateAndClub(currentDate);
                    var salesDictionary = sales
                        .GroupBy(x => (x.Sku, x.Clubs))
                        .ToDictionary(group => group.Key, group => group.Sum(y => y.Sales));

                    var inventories = await _inventory.GetInventoriesByDateAndClubs(currentDate);
                    var inventoriesDictionary = inventories.ToDictionary(x => (x.Sku, x.Clubs), y => y.Inventory);

                    var adsClubs = await _context.TotalAdsClubs
                        .Where(x => x.StartDate == $"{currentDate:yyyy-MM-dd HH:mm:ss.fff}")
                        .OrderBy(z => z.Clubs)
                        .OrderBy(a => a.Sku)
                        .Select(y =>
                           new AdsClubReportDto
                           {
                               Divisor = y.Divisor,
                               Ads = y.Divisor,
                               Date = currentDate.ToString("M/d/yyyy"),
                               Clubs = y.Clubs,
                               Sku = y.Sku
                           })
                        .ToListAsync();

                    foreach (var adsItem in adsClubs)
                    {
                        salesDictionary.TryGetValue((adsItem.Sku, adsItem.Clubs), out var salesToday);
                        inventoriesDictionary.TryGetValue((adsItem.Sku, adsItem.Clubs), out var InventoryToday);

                        adsItem.Sales = salesToday;
                        adsItem.OnHand = InventoryToday;
                    }

                    adsClubReportDtos.AddRange(adsClubs);
                }

                var groupBy = adsClubReportDtos.GroupBy(x => x.Date);

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
    }
}
