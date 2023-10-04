using ads.Models.Data;

namespace ads.Interface
{
    public interface IAds
    {
        Task<List<TotalADS>> GetComputation(string startDate);
        Task<List<TotalADS>> GetTotalApdAsync(List<Inventory> listInventoryResult, List<Sale> listSalesResult, string dateListString);
        Task<List<TotalADS>> GetTotalSkuAndClubsAsync(List<Inventory> listInventoryResult, List<Sale> listSalesResult, string dateListString);
        Task ComputeAds();
    }
}
