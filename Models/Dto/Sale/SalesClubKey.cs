namespace ads.Models.Dto.Sale
{
    public class SalesClubKey : SalesKey
    {
        public string Club { get; set; } = string.Empty;

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var otherKey = (SalesClubKey)obj;

            return Sku == otherKey.Sku && Date == otherKey.Date && Club == otherKey.Club;
        }

        public override int GetHashCode()
        {
            return Sku.GetHashCode() ^ Date.GetHashCode() ^ Club.GetHashCode();
        }
    }
}
