using ads.Interface;
using DocumentFormat.OpenXml.Spreadsheet;
using OfficeOpenXml;
using System.Reflection;

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

        public ExcelRepo(
            ISales sales,
            IInventory inventory,
            ITotalAdsChain totalAdsChain,
            ITotalAdsClub totalAdsClub,
            IClub club,
            IItem item)
        {
            _sales = sales;
            _inventory = inventory;
            _totalAdsChain = totalAdsChain;
            _totalAdsClub = totalAdsClub;
            _club = club;
            _item = item;
        }
        public byte[] ExportDataToExcelByDate<T>(IEnumerable<IGrouping<DateTime, T>> data)
        {
            try
            {
                //Type myType = typeof(T);
                //PropertyInfo[] myProp = myType.GetProperties();

                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.Commercial;
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Sheet1");

                    //foreach (var item in myProp)
                    //{
                    //    var count = 1;
                    //    worksheet.Cells[$"A{count}"].Value = item.Name;
                    //    count++;
                    //}

                    var data2 = data.ToList();

                    foreach (var item in data2)
                    {
                       worksheet.Cells.LoadFromCollection(item, true);
                    }

                    return package.GetAsByteArray();
                }

            }
            catch (Exception error)
            {

                throw;
            }
        }
    }
}
