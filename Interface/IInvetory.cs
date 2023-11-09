using ads.Data;
using ads.Models.Data;

namespace ads.Interface
{
    public interface IInvetory
    {
        Task<List<Inv>> GetInventoryAsync(string start, string end, List<GeneralModel> skus, List<GeneralModel> sales, List<GeneralModel> inventory);
        Task<List<Inv>> ListInv(string dateListString, OledbCon db);
        string TotalInventory(string startDate, string endDate);
        Task<int> CountInventory(string dateListString, OledbCon db);
        Task<List<Inv>> GetInventoriesByDate(DateTime date);
        Dictionary<string, decimal> GetDictionayOfTotalInventory(List<Inv> inventories);
        Dictionary<string, decimal> GetDictionayOfPerClubhlInventory(List<Inv> inventories);
        Task<List<Inv>> GetEFInventoriesByDate(DateTime date);
    }
}
