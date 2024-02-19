using ads.Models.Data;

namespace ads.Interface;

public interface IPrice
{
    Task GetHistoricalPriceFromCsv(DateTime dateTime);
    Task BatchCreatePrices(IEnumerable<Price> data);
    Task<List<Price>> FetchSalesFromMmsByDateAsync(DateTime dateTime);
    Task<List<Price>> GetPricesByDateAsync(DateTime dateTime);
}