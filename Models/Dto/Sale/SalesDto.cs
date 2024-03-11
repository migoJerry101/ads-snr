namespace ads.Models.Dto.Sale
{
    public class SalesDto
    {
        public string Clubs { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public decimal Sales { get; set; }
        public DateTime Date { get; set; }
    }
}
