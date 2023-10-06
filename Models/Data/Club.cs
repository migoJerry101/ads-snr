namespace ads.Models.Data
{
    public class Club
    {
        public int Id { get; set; }
        public int Number { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
    }
}
