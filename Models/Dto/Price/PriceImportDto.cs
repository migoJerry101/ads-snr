using ads.Models.Data;

namespace ads.Models.Dto.Price
{
    public class PriceImportDto
    {
        public string Sku { get; set; } = string.Empty;
        public string Club { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public DateTime Date { get; set; }
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var otherKey = (PriceImportDto)obj;

            return Sku == otherKey.Sku && Club == otherKey.Club;
        }

        public override int GetHashCode()
        {
            return Sku.GetHashCode() ^ Club.GetHashCode();
        }
    }
}
