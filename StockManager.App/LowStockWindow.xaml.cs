using System.Windows;
using Microsoft.EntityFrameworkCore;
using StockManager.Data;

namespace StockManager.App;

public partial class LowStockWindow : Window
{
    public LowStockWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        using var db = new AppDbContext();

        var stocks = await db.StockMovements
            .GroupBy(m => m.ProductId)
            .Select(g => new { Id = g.Key, Qty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.Id, x => x.Qty);

        var products = await db.Products.Where(p => p.IsActive).ToListAsync();

        var rows = products
            .Select(p => new { p.Sku, p.Name, Stock = stocks.GetValueOrDefault(p.Id), p.ReorderLevel })
            .Where(r => r.Stock <= r.ReorderLevel)
            .OrderBy(r => r.Stock)
            .ToList();

        LowGrid.ItemsSource = rows;
        CountText.Text = $"{rows.Count} item(s) at or below reorder level";
    }
}