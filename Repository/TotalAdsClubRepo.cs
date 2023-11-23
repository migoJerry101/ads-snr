using ads.Data;
using ads.Interface;
using ads.Models.Data;
using ads.Models.Dto;
using Microsoft.EntityFrameworkCore;
using static System.Reflection.Metadata.BlobBuilder;
using System.Data;
using System.Data.SqlClient;

namespace ads.Repository
{
    public class TotalAdsClubRepo : ITotalAdsClub
    {
        private readonly AdsContex _context;
        private readonly ILogs _logs;
        private readonly IConfiguration _configuration;

        public TotalAdsClubRepo(AdsContex context, ILogs logs, IConfiguration configuration)
        {
            _context = context;
            _logs = logs;
            _configuration = configuration;
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
            var ads = await _context.TotalAdsClubs.Where(x=> x.StartDate == date).ToListAsync();

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
    }
}
