using ads.Data;
using ads.Models.Data;
using ads.Models.Dto.Inventory;
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
        Dictionary<SalesKey, decimal> GetDictionayOfTotalInventory(List<InventoryDto> inventories);
        Dictionary<string, decimal> GetDictionayOfPerClubhlInventory(List<Inv> inventories);
        Task<List<Inv>> GetEFInventoriesByDate(DateTime date);
        Task BatchUpdateInventoryBysales(List<SalesDto> updatedSales);
        Task<List<InventoryDto>> GetInventoriesByDateEf(DateTime date);
        Task<List<InventoryDto>> GetInventoriesByDateAndClubs(DateTime date);
        Task<List<Inv>> GetInventoriesByDates(List<DateTime> dates);
        Task<List<InventoryDto>> GetInventoriesWithFilteredSku(Dictionary<string, List<string>> sku, List<DateTime> days);
        Task<List<InventoryDto>> GetInventoriesByDateAndClubs(DateTime date, IEnumerable<int> skus);
        Task ImportInventoryBackUpByDate(DateTime date);
    }
}
