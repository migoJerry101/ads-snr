using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ads.Models
{
    [Table("tbl_inv")]
    public class InventoryEntity
    {
        [Key]
        public int Id { get; set; }
        public string Date { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public decimal Inventory { get; set; }
    }
}
