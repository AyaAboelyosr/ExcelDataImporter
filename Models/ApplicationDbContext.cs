using Microsoft.EntityFrameworkCore;

namespace ExcelDataImporter_.Models
{
    public class ApplicationDbContext :DbContext
    {
        public DbSet<Product> Products { get; set; }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {

            this.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));

        }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
         
            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(e => e.ListPrice).HasPrecision(18, 2);
                entity.Property(e => e.MinDiscount).HasPrecision(18, 2);
                entity.Property(e => e.DiscountPrice).HasPrecision(18, 2);
            });

          
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.PartSKU)
                .HasDatabaseName("IX_Product_PartSKU"); 

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.CategoryCode)
                .HasDatabaseName("IX_Product_CategoryCode"); 

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Manufacturer)
                .HasDatabaseName("IX_Product_Manufacturer");  

          
            modelBuilder.Entity<Product>()
                .HasIndex(p => new { p.PartSKU, p.CategoryCode })
                .HasDatabaseName("IX_Product_PartSKU_CategoryCode");

            base.OnModelCreating(modelBuilder);
        }

    }


}
