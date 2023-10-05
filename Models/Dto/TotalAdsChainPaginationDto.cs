namespace ads.Models.Dto
{
    public class TotalAdsChainPaginationDto
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string Club { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
    }
}
