namespace ads.Models.Dto.AdsClub
{
    public class AdsClubCreateDto
    {
        public int Divisor { get; set; }
        public decimal Ads { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Clubs { get; set; } = string.Empty;
        public decimal Sales { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public decimal OverallSales { get; set; }
    }
}
