using ads.Data;
using ads.Models.Data;
using ads.Models.Dto.ItemsDto;
using ads.Models.Dto.Sale;

namespace ads.Interface
{
    public interface IInventory
    {
        Task<List<Inv>> GetInventoryAsync(string start, string end, IEnumerable<ItemSkuDateDto> skus, List<GeneralModel> sales, List<GeneralModel> inventory);
        Task<List<Inv>> ListInv(string dateListString, OledbCon db);
        string TotalInventory(string startDate, string endDate);
        Task<int> CountInventory(string dateListString, OledbCon db);
        Task<List<Inv>> GetInventoriesByDate(DateTime date);
        Dictionary<SalesKey, decimal> GetDictionayOfTotalInventory(List<Inv> inventories);
        Dictionary<string, decimal> GetDictionayOfPerClubhlInventory(List<Inv> inventories);
        Task<List<Inv>> GetEFInventoriesByDate(DateTime date);
        Task BatchUpdateInventoryBysales(List<Sale> updatedSales);
        Task<List<Inv>> GetInventoriesByDateEf(DateTime date);
        Task<List<Inv>> GetInventoriesByDateAndClubs(DateTime date);
        Task<List<Inv>> GetInventoriesByDates(List<DateTime> dates);
    }
}
