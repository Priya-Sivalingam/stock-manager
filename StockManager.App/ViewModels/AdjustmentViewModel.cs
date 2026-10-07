using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using StockManager.Core;
using StockManager.Data;

namespace StockManager.App.ViewModels;

public record AdjustmentRow(string Product, int Change, string? Reason, DateTime Date);

public partial class AdjustmentViewModel : ObservableObject
{
    public ObservableCollection<Product> Products { get; } = new();
    public ObservableCollection<AdjustmentRow> Recent { get; } = new();

    [ObservableProperty] private Product? selectedProduct;
    [ObservableProperty] private int currentStock;
    [ObservableProperty] private int countedQty;
    [ObservableProperty] private string reason = "";
    [ObservableProperty] private string message = "";

    partial void OnSelectedProductChanged(Product? value) => _ = LoadStockAsync();

    public async Task InitAsync()
    {
        using var db = new AppDbContext();
        Products.Clear();
        foreach (var p in await db.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync())
            Products.Add(p);
        await LoadRecentAsync();
    }

    private async Task LoadStockAsync()
    {
        if (SelectedProduct is null) { CurrentStock = 0; CountedQty = 0; return; }
        using var db = new AppDbContext();
        CurrentStock = await db.StockMovements
            .Where(m => m.ProductId == SelectedProduct.Id)
            .SumAsync(m => m.Quantity);
        CountedQty = CurrentStock;
    }

    private async Task LoadRecentAsync()
    {
        using var db = new AppDbContext();
        var rows = await db.StockMovements
            .Where(m => m.Type == MovementType.Adjustment)
            .OrderByDescending(m => m.Id).Take(20)
            .Select(m => new AdjustmentRow(m.Product!.Name, m.Quantity, m.Reason, m.CreatedAt))
            .ToListAsync();
        Recent.Clear();
        foreach (var r in rows) Recent.Add(r);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedProduct is null) { Message = "Select a product."; return; }
        if (CountedQty < 0) { Message = "Counted quantity cannot be negative."; return; }
        if (string.IsNullOrWhiteSpace(Reason)) { Message = "Enter a reason."; return; }

        using var db = new AppDbContext();
        var stock = await db.StockMovements
            .Where(m => m.ProductId == SelectedProduct.Id)
            .SumAsync(m => m.Quantity);

        var diff = CountedQty - stock;
        if (diff == 0) { Message = "No difference. Nothing to adjust."; return; }

        db.StockMovements.Add(new StockMovement
        {
            ProductId = SelectedProduct.Id,
            Type = MovementType.Adjustment,
            Quantity = diff,
            UnitPrice = SelectedProduct.CostPrice,
            Reason = Reason.Trim(),
            CreatedBy = Environment.UserName
        });
        await db.SaveChangesAsync();

        Message = $"{SelectedProduct.Name}: {stock} to {CountedQty} ({diff:+#;-#;0}).";
        Reason = "";
        await LoadStockAsync();
        await LoadRecentAsync();
    }
}