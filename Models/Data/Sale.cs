namespace ads.Models.Data
{
    public class Sale
    {
        public int Id { get; set; }
        public string? Clubs { get; set; }
        public string? Sku { get; set; }
        public decimal Sales { get; set; }
        public DateTime Date { get; set; }
    }
}
