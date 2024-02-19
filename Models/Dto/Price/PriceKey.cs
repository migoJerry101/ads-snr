using ads.Models.Dto.Sale;

namespace ads.Models.Dto.Price
{
    public class PriceKey
    {
        public string Sku { get; set; } = string.Empty;
        public string Club { get; set; } = string.Empty;

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var otherKey = (PriceKey)obj;

            return Sku == otherKey.Sku && Club == otherKey.Club;
        }

        public override int GetHashCode()
        {
            return Sku.GetHashCode() ^ Club.GetHashCode();
        }
    }
}
