using ads.Models.Data;

namespace ads.Interface
{
    public interface IPowerBiAdsClub
    {
        Task SavePowerBiClubAsync(List<PowerBiAdsClub> ads, DateTime date);
        Task<List<PowerBiAdsClub>> GetPowerBiAdsClubByDateAsync(DateTime date);
    }
}
