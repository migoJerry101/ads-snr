using ads.Models.Dto.AdsClub;

namespace ads.Interface
{
    public interface IExcel
    {
        byte[] ExportDataToExcelByDate<T>(IEnumerable<IGrouping<string, T>> data);
    }
}
