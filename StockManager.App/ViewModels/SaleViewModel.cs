using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using StockManager.Core;
using StockManager.Data;

namespace StockManager.App.ViewModels;

public partial class CartLine : ObservableObject
{
    public Product Product { get; init; } = null!;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineTotal))]
    private int quantity;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineTotal))]
    private decimal unitPrice;

    public decimal LineTotal => Quantity * UnitPrice;
}

public partial class SaleViewModel : ObservableObject
{
    public ObservableCollection<CartLine> Cart { get; } = new();

    [ObservableProperty] private CartLine? selectedLine;
    [ObservableProperty] private string scanText = "";
    [ObservableProperty] private int addQty = 1;
    [ObservableProperty] private bool isWholesale;
    [ObservableProperty] private decimal total;
    [ObservableProperty] private string message = "";

    partial void OnIsWholesaleChanged(bool value)
    {
        foreach (var l in Cart)
            l.UnitPrice = value ? l.Product.WholesalePrice : l.Product.RetailPrice;
        RecalcTotal();
    }

    private void RecalcTotal() => Total = Cart.Sum(l => l.LineTotal);

    private static Task<int> StockAsync(AppDbContext db, int productId) =>
        db.StockMovements.Where(m => m.ProductId == productId).SumAsync(m => m.Quantity);

    [RelayCommand]
    private async Task AddItemAsync()
    {
        var code = ScanText.Trim();
        if (code == "") return;
        if (AddQty <= 0) { Message = "Quantity must be at least 1."; return; }

        using var db = new AppDbContext();
        var p = await db.Products.FirstOrDefaultAsync(x => x.IsActive && (x.Barcode == code || x.Sku == code));
        if (p == null) { Message = $"No product found for '{code}'."; return; }

        var line = Cart.FirstOrDefault(l => l.Product.Id == p.Id);
        var wanted = (line?.Quantity ?? 0) + AddQty;
        var stock = await StockAsync(db, p.Id);
        if (wanted > stock) { Message = $"Only {stock} in stock for {p.Name}."; return; }

        if (line == null)
            Cart.Add(new CartLine
            {
                Product = p,
                Quantity = AddQty,
                UnitPrice = IsWholesale ? p.WholesalePrice : p.RetailPrice
            });
        else
            line.Quantity = wanted;

        RecalcTotal();
        ScanText = "";
        AddQty = 1;
        Message = "";
    }

    [RelayCommand]
    private void RemoveLine()
    {
        if (SelectedLine == null) return;
        Cart.Remove(SelectedLine);
        RecalcTotal();
    }

    [RelayCommand]
    private async Task CompleteSaleAsync()
    {
        if (Cart.Count == 0) { Message = "Cart is empty."; return; }

        using var db = new AppDbContext();
        using var tx = await db.Database.BeginTransactionAsync();

        foreach (var l in Cart)
        {
            var stock = await StockAsync(db, l.Product.Id);
            if (l.Quantity > stock) { Message = $"Only {stock} left for {l.Product.Name}."; return; }
        }

        var invoice = "S-" + DateTime.Now.ToString("yyyyMMddHHmmss");
        foreach (var l in Cart)
            db.StockMovements.Add(new StockMovement
            {
                ProductId = l.Product.Id,
                Type = MovementType.Sale,
                Quantity = -l.Quantity,
                UnitPrice = l.UnitPrice,
                Reference = invoice,
                CreatedBy = Environment.UserName
            });

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        Message = $"Sale {invoice} completed. Total: {Total:N2}";
        Cart.Clear();
        RecalcTotal();
    }
}