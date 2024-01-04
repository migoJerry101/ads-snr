using Microsoft.SqlServer.Server;
using System.Globalization;

namespace ads.Utility
{
    public static class DateComputeUtility
    {
        //List of Date within 56 days 
        public static List<string> DateCompute(DateTime startDate)
        {
            List<string> listDate = new List<string>();

            DateTime endDate = startDate.AddDays(-1);
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

        public static List<DateTime> GetDatesInRange(DateTime startDate, DateTime endDate)
        {
            List<DateTime> datesInRange = new List<DateTime>();

            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                datesInRange.Add(date);
            }

            return datesInRange;
        }

        public static int GetDifferenceInRange(string startDate, string endDate)
        {
            string format = "yyyy-MM-dd HH:mm:ss.fff";
            DateTime.TryParseExact(startDate, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime startDateCheckOut);
            DateTime.TryParseExact(endDate, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime endDateCheckOut);
            TimeSpan differenceOut = startDateCheckOut.AddDays(1) - endDateCheckOut;

            return differenceOut.Days;
        }

        public static int GetDifferenceInRange(DateTime startDate, DateTime endDate)
        {
            TimeSpan differenceOut = startDate.AddDays(1) - endDate;

            return differenceOut.Days;
        }
    }
}
