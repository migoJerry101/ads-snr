using ads.Data;
using ads.Interface;
using ads.Models.Data;
using ads.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace ads.Repository
{
    public class TotalAdsClubRepo : ITotalAdsClub
    {
        private readonly AdsContex _context;

        public TotalAdsClubRepo(AdsContex context)
        {
            _context = context;
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
            var ads = _context.TotalAdsClubs;
            var itemsToRemove = ads.Where(x => x.StartDate == date);
            ads.RemoveRange(itemsToRemove);

            await _context.SaveChangesAsync();
        }
    }
}
