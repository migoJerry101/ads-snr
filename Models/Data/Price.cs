namespace ads.Models.Data;

public class Price
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Clubs { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public DateTime CreatedDate { get; set; }
}