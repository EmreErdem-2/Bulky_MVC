using BulkyWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace BulkyWeb.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        public DbSet<Category> Categories { get; set; }
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    modelBuilder.Entity<BulkyWeb.Models.Category>().HasData(
        //        new BulkyWeb.Models.Category
        //        {
        //            Id = 1,
        //            Name = "Action",
        //            DisplayOrder = 1
        //        },
        //        new BulkyWeb.Models.Category
        //        {
        //            Id = 2,
        //            Name = "Drama",
        //            DisplayOrder = 2
        //        }
        //    );
        //    base.OnModelCreating(modelBuilder);
        //}
    }
}
