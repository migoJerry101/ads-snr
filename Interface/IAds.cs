using ads.Models.Data;

namespace ads.Interface
{
    public interface IAds
    {
        Task<List<TotalAdsChain>> GetComputation(string startDate);
        Task<List<TotalAdsChain>> GetTotalApdAsync(List<Inv> listInventoryResult, List<Sale> listSalesResult, string dateListString);
        Task<List<TotalAdsClub>> GetTotalSkuAndClubsAsync(List<Inv> listInventoryResult, List<Sale> listSalesResult, string dateListString);
        Task ComputeAds();
    }
}
