namespace ads.Models.Dto.Price;

public class PriceCsvDto
{
    public DateTime Transaction_Date { get; set; }
    public string SKU_Number { get; set; } = string.Empty;
    public string Store_Number { get; set; } = string.Empty;
    public string Total_Sales { get; set; } = string.Empty;
    public string Total_Quantity_Sold { get; set; } = string.Empty;
}