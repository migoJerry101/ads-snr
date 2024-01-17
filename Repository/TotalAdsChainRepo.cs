using ads.Data;
using ads.Interface;
using ads.Models.Data;
using Microsoft.EntityFrameworkCore;
using static System.Reflection.Metadata.BlobBuilder;
using System.Data;
using System.Data.SqlClient;

namespace ads.Repository
{
    public class TotalAdsChainRepo : ITotalAdsChain
    {
        private readonly AdsContex _context;
        private readonly ILogs _logs;
        private readonly IConfiguration _configuration;

        public TotalAdsChainRepo(AdsContex context, ILogs logs, IConfiguration configuration)
        {
            _context = context;
            _logs = logs;
            _configuration = configuration;
        }
        public TotalAdsChain GetTotalAdsChain()
        {
            var totalAdsChain = _context.TotalAdsChains.FirstOrDefault();

            return totalAdsChain;
        }

        public async Task<List<TotalAdsChain>> GetTotalAdsChainByDate(string date)
        {
            var ads = await _context.TotalAdsChains
                .Where(x => x.StartDate == date)
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
    }
}
