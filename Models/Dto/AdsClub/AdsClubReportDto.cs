﻿namespace ads.Models.Dto.AdsClub
{
    public class AdsClubReportDto
    {
        public string Clubs { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public decimal OnHand { get; set; }
        public decimal Sales { get; set; }
        public int Divisor { get; set; }
        public decimal Ads { get; set; }
        public string Date { get; set; } = string.Empty;
    }
}
