using ads.Models.Dto.Price;

namespace ads.Models.Dto.Condtx
{
    public class CondtxKey
    {
        public string Sku { get; set; } = string.Empty;
        public string Club { get; set; } = string.Empty;

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var otherKey = (CondtxKey)obj;

            return Sku == otherKey.Sku && Club == otherKey.Club;
        }

        public override int GetHashCode()
        {
            return Sku.GetHashCode() ^ Club.GetHashCode();
        }
    }
}
