using ads.Models.Data;

namespace ads.Interface
{
    public interface IAds
    {
        Task<List<TotalAdsChain>> GetComputation(string startDate);
        Task<List<TotalAdsChain>> GetTotalApdAsync(List<Inventory> listInventoryResult, List<Sale> listSalesResult, string dateListString);
        Task<List<TotalAdsClub>> GetTotalSkuAndClubsAsync(List<Inventory> listInventoryResult, List<Sale> listSalesResult, string dateListString);
        Task ComputeAds();
    }
}
