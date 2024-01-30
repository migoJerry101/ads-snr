namespace ads.Models.Data;

public class Price
{
    public int Id { get; set; }
    public int Sku { get; set; }
    public int Club { get; set; }
    public decimal Value { get; set; }
    public DateTime CreatedDate { get; set; }
}