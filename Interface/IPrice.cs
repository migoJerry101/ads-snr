using ads.Models.Data;
using ads.Models.Dto.Price;

namespace ads.Interface;

public interface IPrice
{
    Task GetHistoricalPriceFromCsv(DateTime dateTime);
    Task BatchCreatePrices(IEnumerable<Price> data);
    Task FetchSalesFromMmsByDateAsync();
    Task<List<PriceDto>> GetPricesByDateAsync(DateTime dateTime);
    Task DeletePriceByDate(DateTime date);
}