using ads.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace ads.Data
{
    public class AdsContext : DbContext
    {
        public AdsContext(DbContextOptions<AdsContext> options) : base(options)
        {
        }

        public DbSet<Sale> Sales { get; set; }
        public DbSet<Inv> Inventories { get; set; }
        public DbSet<TotalAdsChain> TotalAdsChains { get; set; }
        public DbSet<TotalAdsClub> TotalAdsClubs { get; set;}
        public DbSet<Club> Clubs { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Price> Prices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Sale>().ToTable("tbl_sales_data");
            modelBuilder.Entity<Inv>().ToTable("tbl_inv");
            modelBuilder.Entity<TotalAdsChain>().ToTable("tbl_totalAds");
            modelBuilder.Entity<TotalAdsClub>().ToTable("tbl_totaladsperclubs");
            modelBuilder.Entity<Club>().ToTable("tbl_Clubs");
            modelBuilder.Entity<Item>().ToTable("tbl_Items");
            modelBuilder.Entity<Price>().ToTable("tbl_Prices");
        }
    }
}
