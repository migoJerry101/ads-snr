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
        public DbSet<Inv> Inventories { get; set; }
        public DbSet<TotalAdsChain> TotalAdsChains { get; set; }
        public DbSet<TotalAdsClub> TotalAdsClubs { get; set;}
        public DbSet<Club> Clubs { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<TagClub> TagClubs { get; set; }
        public DbSet<TagChain> TagChains { get; set; }
        public DbSet<Logging> Logs { get; set; }
        public DbSet<PowerBiAdsChain> PowerBiAdsChains { get; set; }
        public DbSet<PowerBiAdsClub> PowerBiAdsClubs { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Sale>().ToTable("tbl_sales_data");
            modelBuilder.Entity<Inv>().ToTable("tbl_inv");
            modelBuilder.Entity<TotalAdsChain>().ToTable("tbl_totalAds");
            modelBuilder.Entity<TotalAdsClub>().ToTable("tbl_totaladsperclubs");
            modelBuilder.Entity<Club>().ToTable("tbl_Clubs");
            modelBuilder.Entity<Item>().ToTable("tbl_Items");
            modelBuilder.Entity<TagClub>().ToTable("tbl_tagClubs");
            modelBuilder.Entity<TagClub>().ToTable("tbl_tagChains");
            modelBuilder.Entity<Logging>().ToTable("tbl_logs");
            modelBuilder.Entity<PowerBiAdsChain>().ToTable("tbl_powerBiAdsChains");
            modelBuilder.Entity<PowerBiAdsClub>().ToTable("tbl_powerBiAdsClubs");
        }
    }
}
