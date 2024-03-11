using ads.Data;
using ads.Models.Data;
using ads.Models.Dto.ItemsDto;
using ads.Models.Dto.Sale;

namespace ads.Interface
{
    public interface ISales
    {
        Task<List<Sale>> GetSalesAsync(string start, string end, IEnumerable<ItemSkuDateDto> skus, List<GeneralModel> listOfSales, List<GeneralModel> inventories);
        Task<List<Sale>> ListSales(string dateListString, OledbCon db);
        string TotalSales(string startDate, string endDate);
        Task<int> CountSales(string dateListString, OledbCon db);
        Task<List<Sale>> GetSalesByDate(DateTime date);
        Dictionary<string, decimal> GetDictionayOfTotalSales(List<SalesDto> sales);
        Task DeleteSalesByDateAsync(DateTime date);
        List<SalesDto> GetAdjustedSalesValue(List<SalesDto> sales, IEnumerable<Sale> reImportedSales);
        Task<List<SalesDto>> GetSalesByDateEf(DateTime date);
        void DeleteSalesByDate(DateTime date);
        Task<List<SalesDto>> GetSalesByDateAndClub(DateTime date);
        Task<List<Sale>> GetSalesByDates(List<DateTime> dates);
        Dictionary<SalesKey, decimal> GetDictionayOfTotalSalesWithSalesKey(List<SalesDto> sales);
        Task<List<SalesDto>> GetSalesWithFilteredSku(Dictionary<string, List<string>> sku, List<DateTime> days);
    }
}
