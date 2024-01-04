using ads.Models.Data;

namespace ads.Interface
{
    public interface IPowerBiAdsChain
    {
        Task SavePowerBiChainAsync(List<PowerBiAdsChain> ads, DateTime date);
        //get ads by date - list ads
        Task<List<PowerBiAdsChain>> GetPowerBiAdsChainsByDateAsync(DateTime date);
    }
}
