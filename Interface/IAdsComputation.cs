using ads.Models.Data;

namespace ads.Interface
{
    public interface IAdsComputation
    {
        Task<List<TotalAPD>> GetTotalApdAsync();
        List<string> DateCompute(string startDateStr);
        List<DateTime> GetDatesInRange(DateTime startDate, DateTime endDate);
    }
}
