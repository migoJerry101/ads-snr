using System.Globalization;

namespace ads.Utility
{
    public class DateComputeUtility
    {
        //List of Date within 56 days 
        public List<string> DateCompute(string startDateStr)
        {
            List<string> listDate = new List<string>();

            // Get the current date
            DateTime currentDate = DateTime.ParseExact(startDateStr, "yyMMdd", CultureInfo.InvariantCulture);

            // Subtract one day
            DateTime previousDate = currentDate.AddDays(-1);

            DateTime endDate = previousDate;
            DateTime startDate = endDate.AddDays(-55);


            List<DateTime> datesInRange = GetDatesInRange(startDate, endDate);

            foreach (DateTime date in datesInRange)
            {
                Console.WriteLine(date.ToString("yyMMdd"));

                listDate.Add(date.ToString("yyMMdd"));
            }

            return listDate.ToList();
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
