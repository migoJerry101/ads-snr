namespace ads.Models.Dto.AdsChain
{
    public class AdsChainCreateDto
    {
        public int Divisor { get; set; }
        public decimal Ads { get; set; }
        public string Sku { get; set; } = string.Empty;
        public decimal Sales { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
    }
}
