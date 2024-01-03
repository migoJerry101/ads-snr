namespace ads.Interface
{
    public interface IPowerBiAdsChain
    {
        Task SavePowerBiChainAsync(List<IPowerBiAdsChain> ads, DateTime date);
        //get ads by date - list ads
        //get ads by date and sku - single ads
    }
}
