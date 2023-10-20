using ads.Models.Data;
using ads.Models.Dto;

namespace ads.Interface
{
    public interface ITotalAdsChain
    {
        Task<(List<TotalAdsChain>, int totalPages)> GetTotalAdsChain(TotalAdsChainPaginationDto data);
        Task<List<TotalAdsChain>> GetTotalAdsChainByDate(string date);
    }
}
