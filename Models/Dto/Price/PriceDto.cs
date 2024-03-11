namespace ads.Models.Dto.Price
{
    public class PriceDto
    {
        public string Sku { get; set; } = string.Empty;
        public string Club { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public DateTime Date { get; set; }
    }
}
