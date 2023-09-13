namespace ads.Models.Data
{
    public class Logging
    {
        public DateTime StartLog { get; set; }
        public DateTime EndLog { get; set; }
        public string? Action { get; set; }
        public string? Message { get; set; }
        public string? Record_Date { get; set; }

    }
}
