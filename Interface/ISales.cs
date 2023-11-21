using ads.Data;
using ads.Models.Data;
using ads.Models.Dto.ItemsDto;

namespace ads.Interface
{
    public interface ISales
    {
        Task<List<Sale>> GetSalesAsync(string start, string end, IEnumerable<ItemSkuDateDto> skus, List<GeneralModel> listOfSales, List<GeneralModel> inventories);
        Task<List<Sale>> ListSales(string dateListString, OledbCon db);
        string TotalSales(string startDate, string endDate);
        Task<int> CountSales(string dateListString, OledbCon db);
        Task<List<Sale>> GetSalesByDate(DateTime date);
        Dictionary<string, decimal> GetDictionayOfTotalSales(List<Sale> sales);
        Task DeleteSalesByDateAsync(DateTime date);
        List<Sale> GetAdjustedSalesValue(List<Sale> sales, IEnumerable<Sale> reImportedSales);
        Task<List<Sale>> GetSalesByDateEf(DateTime date);
    }
}
