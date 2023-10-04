using ads.Models.Data;
using ads.Models.Dto;

namespace ads.Interface
{
    public interface ITotalAdsClub
    {
        Task<(List<TotalAdsClub>, int totalPages)> GetPaginatedTotalAdsClubs(TotalAdsChainPaginationDto data);
    }
}
