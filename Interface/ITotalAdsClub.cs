using ads.Models.Data;
using ads.Models.Dto.AdsChain;
using ads.Models.Dto.AdsClub;

namespace ads.Interface
{
    public interface ITotalAdsClub
    {
        Task<(List<TotalAdsClub>, int totalPages)> GetPaginatedTotalAdsClubs(TotalAdsChainPaginationDto data);
        Task<List<TotalAdsClub>> GetTotalAdsClubsByDate(string date);
        Task DeleteAdsClubsAsync(string date);
        Task<IEnumerable<IGrouping<string, AdsClubReportDto>>> GenerateAdsClubsReportDto(DateTime startDate, DateTime endDate);
        Task UpdateClubTotalAverageSales(DateTime date);
    }
}
