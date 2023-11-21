using ads.Models.Data;

namespace ads.Interface
{
    public interface ITotalAdsChain
    {
        TotalAdsChain GetTotalAdsChain();
        Task<List<TotalAdsChain>> GetTotalAdsChainByDate(string date);
        Task DeleteAdsChainAsync(string date);
    }
}
