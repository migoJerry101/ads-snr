using ads.Data;
using ads.Models.Data;

namespace ads.Interface
{
    public interface ISales
    {
        Task<List<DataRows>> GetSalesAsync(string start, string end);
        Task<List<DataRows>> ListSales(string dateListString, OledbCon db);
        string TotalSales(string startDate, string endDate);
        Task<int> CountSales(string dateListString, OledbCon db);
    }
}
