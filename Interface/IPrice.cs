using ads.Models.Data;

namespace ads.Interface;

public interface IPrice
{
    Task GetHistoricalPriceFromCsv(DateTime dateTime);
    Task BatchCreatePrices(IEnumerable<Price> data);
    Task FetchSalesFromMmsByDateAsync();
    Task<List<Price>> GetPricesByDateAsync(DateTime dateTime);
    Task DeletePriceByDate(DateTime date);
}