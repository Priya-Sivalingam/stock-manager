using Microsoft.EntityFrameworkCore;
using StockManager.Core;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace StockManager.Data;

public class AppDbContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnConfiguring(DbContextOptionsBuilder o)
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StockManager", "stock.db");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        o.UseSqlite($"Data Source={path}");
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Product>().HasIndex(p => p.Sku).IsUnique();
        b.Entity<Product>().HasIndex(p => p.Barcode);
    }
}