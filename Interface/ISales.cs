using ads.Data;
using ads.Models.Data;

namespace ads.Interface
{
    public interface ISales
    {
        Task<List<Sale>> GetSalesAsync(string start, string end, List<GeneralModel> skus, List<GeneralModel> listOfSales, List<GeneralModel> inventories);
        Task<List<Sale>> ListSales(string dateListString, OledbCon db);
        string TotalSales(string startDate, string endDate);
        Task<int> CountSales(string dateListString, OledbCon db);
        Task<List<Sale>> GetSalesByDate(DateTime date);
        Dictionary<string, decimal> GetDictionayOfTotalSales(List<Sale> sales);
    }
}
