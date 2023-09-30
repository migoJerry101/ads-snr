using ads.Models.Data;

namespace ads.Interface
{
    public interface IData
    {
        Task<List<Sale>> GetDataAsync(string start , string end);
        Task<List<Inventory>> GetInventoryAsync(string start, string end);
        Task<List<TotalADS>> GetTotalApdAsync(string start);
    }
}
