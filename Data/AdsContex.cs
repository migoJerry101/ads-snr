using ads.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace ads.Data
{
    public class AdsContex : DbContext
    {
        public AdsContex(DbContextOptions<AdsContex> options) : base(options)
        {
        }

        public DbSet<Sale> Sales { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<TotalAdsChain> TotalAdsChains { get; set; }
        public DbSet<TotalAdsClub> TotalAdsClubs { get; set;}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Sale>().ToTable("tbl_data");
            modelBuilder.Entity<Inventory>().ToTable("tbl_inv");
            modelBuilder.Entity<TotalAdsChain>().ToTable("tbl_totalAds");
            modelBuilder.Entity<TotalAdsClub>().ToTable("tbl_totaladsperclubs");
        }
    }
}
