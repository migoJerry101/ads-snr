namespace ads.Models.Dto.Price;

public class PriceCsvDto
{
    public int CSDATE { get; set; }
    public int CSSKU { get; set; }
    public int CSSTOR { get; set; }
    public string CSEXPR { get; set; } = string.Empty;
}