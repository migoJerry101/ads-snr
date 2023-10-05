namespace ads.Models.Data
{
    public class TotalAdsChain
    {
        public int Id { get; set; }
        public int Divisor { get; set; }
        public decimal Ads { get; set; }
        public string? Sku { get; set; }
        public decimal Sales { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
    }
}
