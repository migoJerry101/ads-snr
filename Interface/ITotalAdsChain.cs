using ads.Models.Data;
using ads.Models.Dto.AdsChain;
using ads.Models.Dto.AdsClub;

namespace ads.Interface
{
    public interface ITotalAdsChain
    {
        TotalAdsChain GetTotalAdsChain();
        Task<List<AdsChainCreateDto>> GetTotalAdsChainByDate(string date);
        Task DeleteAdsChainAsync(string date);
        Task<IEnumerable<IGrouping<string, AdsChainReportDto>>> GenerateAdsChainReportDto(DateTime startDate, DateTime endDate);
    }
}
