using ads.Data;
using ads.Models.Data;

namespace ads.Interface
{
    public interface IInvetory
    {
        Task<List<Inventory>> GetInventoryAsync(string start, string end, List<GeneralModel> skus, List<GeneralModel> sales, List<GeneralModel> inventory);
        Task<List<Inventory>> ListInv(string dateListString, OledbCon db);
        string TotalInventory(string startDate, string endDate);
        Task<int> CountInventory(string dateListString, OledbCon db);
        Task<List<Inventory>> GetInventoriesByDate(DateTime date);
        Dictionary<string, decimal> GetDictionayOfTotalInventory(List<Inventory> inventories);
    }
}
