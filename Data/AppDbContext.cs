using BusinessManagementAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BusinessManagementAPI.Data;

public class AppDbContext : DbContext
{
    // Constructor for the AppDbContext class
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // DbSet for the Product entity
    // This property represents the Products table in the database
    public DbSet<Product> Products { get; set; }

    // Override the OnModelCreating method to configure the model
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure the precision of the Price property in the Product entity
        // This ensures that the Price column in the database has a precision of 18 and a scale of 2
        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasPrecision(18, 2);
    }
}