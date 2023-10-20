using ads.Data;
using ads.Interface;
using ads.Models.Data;
using ads.Models.Dto;
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
        public async Task<(List<TotalAdsChain>, int totalPages)> GetTotalAdsChain(TotalAdsChainPaginationDto data)
        {
            //implement pagination
            //implement filters
            //implement sort

            var ads =  _context.TotalAdsChains.Where(x =>
                    (string.IsNullOrEmpty(data.Sku) || x.Sku == data.Sku) &&
                    x.StartDate == data.StartDate);

            var adsCount = await ads.CountAsync();
            var totalPages = (int)Math.Ceiling((double)adsCount / data.PageSize);

            var paginatedAds = await ads
                .Skip((data.PageNumber - 1) * data.PageSize)
                .Take(data.PageSize)
                .ToListAsync();


            //var date = "2023-10-11 00:00:00.000";
            //var totalAdsChain = await _context.TotalAdsChains.Where(x => x.StartDate == date).ToListAsync();

            return (paginatedAds, totalPages);
        }

        public async Task<List<TotalAdsChain>> GetTotalAdsChainByDate(string date)
        {
            var ads = await _context.TotalAdsChains.Where(x => x.StartDate == date).ToListAsync();

            return ads;
        }
    }
}
