using ads.Models.Dto.Condtx;

namespace ads.Interface
{
    public interface ICondtx
    {
        Task<IEnumerable<CondtxDto>> FetchTotalSalesFromMmsByDateAsync(DateTime dateTime);
        Dictionary<CondtxKey, decimal> GetTotalSalesDictionary(IEnumerable<CondtxDto> data);
    }
}
