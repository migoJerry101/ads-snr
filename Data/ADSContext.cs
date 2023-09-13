using ads.Models;
using Microsoft.EntityFrameworkCore;

namespace ads.Data
{
    public class ADSContext : DbContext
    {
        public ADSContext(DbContextOptions<ADSContext> options) : base(options)
        {
        }
        public DbSet<DataEntity> DataEF { get; set; }
        public DbSet<InventoryEntity> InventoryEF { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DataEntity>().ToTable("tbl_data");

            modelBuilder.Entity<InventoryEntity>().ToTable("tbl_inv");

        }

    }
}
