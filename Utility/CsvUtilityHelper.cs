using System.Globalization;
using CsvHelper;

namespace ads.Utility;

public class CsvUtilityHelper
{
    public static IEnumerable<T> GetHistoricalListFromCsv<T>(string location)
    {
        using var reader = new StreamReader(location);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var records =  csv.GetRecords<T>().ToList();

        return records;
    }
}