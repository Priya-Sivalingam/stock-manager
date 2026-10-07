using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using StockManager.Core;
using StockManager.Data;

namespace StockManager.App.ViewModels;

public record MovementRow(string Product, int Quantity, decimal UnitCost, string? Reference, DateTime Date);

public partial class ReceiptViewModel : ObservableObject
{
    public ObservableCollection<Product> Products { get; } = new();
    public ObservableCollection<MovementRow> Recent { get; } = new();

    [ObservableProperty] private Product? selectedProduct;
    [ObservableProperty] private int quantity;
    [ObservableProperty] private decimal unitCost;
    [ObservableProperty] private string? reference;
    [ObservableProperty] private int currentStock;
    [ObservableProperty] private string message = "";

    partial void OnSelectedProductChanged(Product? value)
    {
        UnitCost = value?.CostPrice ?? 0;
        _ = LoadStockAsync();
    }

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
        if (SelectedProduct is null) { CurrentStock = 0; return; }
        using var db = new AppDbContext();
        CurrentStock = await db.StockMovements
            .Where(m => m.ProductId == SelectedProduct.Id)
            .SumAsync(m => m.Quantity);
    }

    private async Task LoadRecentAsync()
    {
        using var db = new AppDbContext();
        var rows = await db.StockMovements
            .Where(m => m.Type == MovementType.Receipt)
            .OrderByDescending(m => m.Id).Take(20)
            .Select(m => new MovementRow(m.Product!.Name, m.Quantity, m.UnitPrice, m.Reference, m.CreatedAt))
            .ToListAsync();
        Recent.Clear();
        foreach (var r in rows) Recent.Add(r);
    }

    [RelayCommand]
    private async Task ReceiveAsync()
    {
        if (SelectedProduct is null) { Message = "Select a product."; return; }
        if (Quantity <= 0) { Message = "Quantity must be greater than 0."; return; }
        if (UnitCost < 0) { Message = "Cost cannot be negative."; return; }

        using var db = new AppDbContext();
        db.StockMovements.Add(new StockMovement
        {
            ProductId = SelectedProduct.Id,
            Type = MovementType.Receipt,
            Quantity = Quantity,
            UnitPrice = UnitCost,
            Reference = string.IsNullOrWhiteSpace(Reference) ? null : Reference.Trim(),
            CreatedBy = Environment.UserName
        });

        // keep the product's latest cost price up to date
        var product = await db.Products.FindAsync(SelectedProduct.Id);
        if (product != null) product.CostPrice = UnitCost;

        await db.SaveChangesAsync();

        Message = $"Received {Quantity} x {SelectedProduct.Name}.";
        Quantity = 0;
        Reference = null;
        await LoadStockAsync();
        await LoadRecentAsync();
    }
}