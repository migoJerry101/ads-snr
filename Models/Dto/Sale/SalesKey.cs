using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ads.Models.Dto.Sale
{
    public class SalesKey
    {
        public string Sku { get; set; } = string.Empty;
        public DateTime Date { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var otherKey = (SalesKey)obj;

            return Sku == otherKey.Sku && Date == otherKey.Date;
        }

        public override int GetHashCode()
        {
            return Sku.GetHashCode() ^ Date.GetHashCode();
        }
    }
}
