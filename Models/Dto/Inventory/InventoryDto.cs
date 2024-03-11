namespace ads.Models.Dto.Inventory
{
    public class InventoryDto
    {
        public DateTime Date { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Clubs { get; set; } = string.Empty;
        public decimal Inventory { get; set; }
    }
}
