using System.Globalization;

namespace ads.Utility
{
    public class DateComputeUtility
    {
        //List of Date within 56 days 
        public List<string> DateCompute(DateTime startDate)
        {
            List<string> listDate = new List<string>();

            DateTime endDate = startDate.AddDays(-3);
            //DateTime endDate = startDate.AddDays(-55);

            // Iterate through the date range
            while (startDate > endDate)
            {
                DateTime dateWithZeroTime = new DateTime(startDate.Year, startDate.Month, startDate.Day, 0, 0, 0, 0);
                listDate.Add(dateWithZeroTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                startDate = startDate.AddDays(-1); // Increment the startDate by one day
            }

            return listDate;
        }
        public List<DateTime> GetDatesInRange(DateTime startDate, DateTime endDate)
        {
            List<DateTime> datesInRange = new List<DateTime>();

            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                datesInRange.Add(date);
            }

            return datesInRange;
        }
    }
}
