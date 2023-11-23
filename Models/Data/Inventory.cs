namespace ads.Models.Data
{
    public class Inv
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string? Sku { get; set; }
        public string? Clubs { get; set; }
        public decimal Inventory { get; set; }
    }
}
