using System.Globalization;

namespace ads.Utility
{
    public static class DateConvertion
    {
        public static DateTime ConvertStringDate(string? date)
        {
            // Input string in the format "yyMMdd"
            string dateString = date; // for September 20, 2023

            // Define the format you expect
            string format = "yyMMdd";

            // Specify the culture (optional)
            CultureInfo culture = CultureInfo.InvariantCulture;

            try
            {
                // Parse the string into a DateTime object
                DateTime dateTime = DateTime.ParseExact(dateString, format, culture);

                // Output the resulting DateTime
                Console.WriteLine("Parsed DateTime: " + dateTime.ToString("yyyy-MM-dd"));

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
