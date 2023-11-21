using ads.Data;
using ads.Interface;
using ads.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace ads.Repository
{
    public class TotalAdsChainRepo : ITotalAdsChain
    {
        private readonly AdsContex _context;

        public TotalAdsChainRepo(AdsContex context)
        {
            _context = context;
        }
        public TotalAdsChain GetTotalAdsChain()
        {
            var totalAdsChain = _context.TotalAdsChains.FirstOrDefault();

            return totalAdsChain;
        }

        public async Task<List<TotalAdsChain>> GetTotalAdsChainByDate(string date)
        {
            var ads = await _context.TotalAdsChains.Where(x => x.StartDate == date).ToListAsync();

            return ads;
        }

        public async Task DeleteAdsChainAsync(string date)
        {
            var ads = _context.TotalAdsChains;
            var itemsToRemove = ads.Where(x => x.StartDate == date);
            ads.RemoveRange(itemsToRemove);

            await _context.SaveChangesAsync();
        }
    }
}
