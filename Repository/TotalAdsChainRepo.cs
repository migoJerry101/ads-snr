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
    }
}
