using System.Globalization;

namespace ads.Utility
{
    public static class DateConvertion
    {
        public static DateTime ConvertStringDate(string date)
        {
            string format = "yyMMdd";
            CultureInfo culture = CultureInfo.InvariantCulture;

            try
            {
                // Parse the string into a DateTime object
                DateTime dateTime = DateTime.ParseExact(date, format, culture);

                // Remove the time portion and keep only the date part
                DateTime dateOnly = dateTime.Date;

                return dateOnly;
            }
            catch (FormatException)
            {
                Console.WriteLine("Invalid date format.");

                return DateTime.Now;
            }
        }
    }
}
