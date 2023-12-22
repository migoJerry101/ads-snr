namespace ads.Models.Data
{
    public class TagClub
    {
        public int Id { get; set; }
        public string Club { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public bool IsAdsDivisor { get; set; }
        public bool IsPbiDivisor { get; set; }
        public bool IsOutofStocksWithOutSale { get; set; }
    }
}
