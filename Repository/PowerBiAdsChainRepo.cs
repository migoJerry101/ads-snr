using ads.Data;
using ads.Interface;
using ads.Models.Data;
using Microsoft.EntityFrameworkCore;

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

        public Task SavePowerBiChainAsync(List<PowerBiAdsChain> ads, DateTime date)
        {
            try
            {

            }
            catch (Exception)
            {

                throw;
            }
            throw new NotImplementedException();
        }
    }
}
