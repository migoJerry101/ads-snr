using ads.Interface;
using ads.Models.Data;
using DocumentFormat.OpenXml.Spreadsheet;
using OfficeOpenXml;
using System.Reflection;
using static System.Reflection.Metadata.BlobBuilder;

namespace ads.Repository
{
    public class ExcelRepo : IExcel
    {
        private readonly ISales _sales;
        private readonly IInventory _inventory;
        private readonly ITotalAdsChain _totalAdsChain;
        private readonly ITotalAdsClub _totalAdsClub;
        private readonly IClub _club;
        private readonly IItem _item;
        private readonly ILogs _logs;

        public ExcelRepo(
            ISales sales,
            IInventory inventory,
            ITotalAdsChain totalAdsChain,
            ITotalAdsClub totalAdsClub,
            IClub club,
            IItem item,
            ILogs logs)
        {
            _sales = sales;
            _inventory = inventory;
            _totalAdsChain = totalAdsChain;
            _totalAdsClub = totalAdsClub;
            _club = club;
            _item = item;
            _logs = logs;
        }

        public byte[] ExportDataToExcelByDate<T>(IEnumerable<IGrouping<string, T>> data)
        {
            DateTime startLogs = DateTime.Now;
            List<Logging> log = new List<Logging>();

            try
            {
                const int maxRow = 1048576;

                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.Commercial;
                using var package = new ExcelPackage();
                var data2 = data.ToList();

                foreach (var item in data2)
                {
                    var name = item.Key;
                    var count = item.Count();

                    if (count < maxRow)
                    {
                        var worksheet = package.Workbook.Worksheets.Add(name);

                        worksheet.Cells.LoadFromCollection(item, true);
                    }
                    else
                    {
                        int midpoint = count / 2;
                        var models = item.ToList();
                        var firstHalf = models.GetRange(0, midpoint);
                        models.RemoveRange(0, midpoint);

                        var worksheet01 = package.Workbook.Worksheets.Add($"{name}-01");
                        var worksheet02 = package.Workbook.Worksheets.Add($"{name}-02");

                        worksheet01.Cells.LoadFromCollection(firstHalf, true);
                        worksheet02.Cells.LoadFromCollection(models, true);
                    }

                }

                return package.GetAsByteArray();
            }
            catch (Exception error)
            {
                var endLogs = DateTime.Now;

                log.Add(new Logging
                {
                    StartLog = startLogs,
                    EndLog = endLogs,
                    Action = "Error",
                    Message = "Delete Ads Clubs : " + error.Message + " ",
                    Record_Date = endLogs.Date
                });

                _logs.InsertLogs(log);
                throw;
            }
        }
    }
}
