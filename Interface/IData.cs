using ads.Models.Data;

namespace ads.Interface
{
    public interface IData
    {
        Task<List<Sale>> GetDataAsync(string start , string end);
        Task<List<Inv>> GetInventoryAsync(string start, string end);
        Task<List<TotalAdsChain>> GetTotalApdAsync(string start);
    }
}
