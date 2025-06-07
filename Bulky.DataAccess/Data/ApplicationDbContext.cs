using BulkyBook.Models;
using Microsoft.EntityFrameworkCore;

namespace BulkyBook.DataAccess.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Action", DisplayOrder = 1 },
                new Category { Id = 2, Name = "Drama", DisplayOrder = 2 },
                new Category { Id = 3, Name = "Drama", DisplayOrder = 2 }
            );

            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 1, Title = "Lord of the Rings", Description = "", ISBN = "1231412", Author = "Tolkien", ListPrice = 15, Price = 12, Price50 = 10, Price100 = 8 },
                new Product { Id = 2, Title = "Dune", Description = "", ISBN = "4124123", Author = "Herbert", ListPrice = 22, Price = 18, Price50 = 15, Price100 = 12 },
                new Product { Id = 3, Title = "Series of Unfortonate Events", Description = "asdasd", ISBN = "6344533", Author = "Josephg", ListPrice = 17, Price = 16, Price50 = 10, Price100 = 8 },
                new Product { Id = 4, Title = "Name Of the Wind", Description = "", ISBN = "1231412", Author = "Patrick Rothfuss", ListPrice = 33, Price = 30, Price50 = 22, Price100 = 20 }
            );
            base.OnModelCreating(modelBuilder);
        }
    }
}
