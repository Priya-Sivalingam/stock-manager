using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using StockManager.Core;
using StockManager.Data;

namespace StockManager.App.ViewModels;

public partial class ProductsViewModel : ObservableObject
{
    public ObservableCollection<Product> Products { get; } = new();
    public ObservableCollection<Category> Categories { get; } = new();
    public ObservableCollection<Supplier> Suppliers { get; } = new();

    [ObservableProperty] private Product? selectedProduct;
    [ObservableProperty] private Product editing = new();
    [ObservableProperty] private string searchText = "";
    [ObservableProperty] private string scanText = "";
    [ObservableProperty] private string message = "";

    public event Action? NewCodeScanned;

    partial void OnSearchTextChanged(string value) => _ = LoadProductsAsync();

    partial void OnSelectedProductChanged(Product? value)
    {
        if (value is null) return;
        Editing = Copy(value);
        Message = "";
    }

    private static Product Copy(Product v) => new()
    {
        Id = v.Id,
        Sku = v.Sku,
        Barcode = v.Barcode,
        Name = v.Name,
        CategoryId = v.CategoryId,
        SupplierId = v.SupplierId,
        Unit = v.Unit,
        CostPrice = v.CostPrice,
        RetailPrice = v.RetailPrice,
        WholesalePrice = v.WholesalePrice,
        ReorderLevel = v.ReorderLevel,
        IsActive = v.IsActive
    };

    public async Task InitAsync()
    {
        using var db = new AppDbContext();
        await db.Database.MigrateAsync();

        if (!await db.Categories.AnyAsync())
        {
            db.Categories.Add(new Category { Name = "General" });
            await db.SaveChangesAsync();
        }

        await ReloadListsAsync();
        await LoadProductsAsync();
    }

    // Reloads the category and supplier dropdowns, then clears the form
    public async Task ReloadListsAsync()
    {
        using var db = new AppDbContext();

        Categories.Clear();
        foreach (var c in await db.Categories.OrderBy(c => c.Name).ToListAsync())
            Categories.Add(c);

        Suppliers.Clear();
        Suppliers.Add(new Supplier { Id = 0, Name = "(none)" });
        foreach (var s in await db.Suppliers.OrderBy(s => s.Name).ToListAsync())
            Suppliers.Add(s);

        NewProduct();
    }

    private async Task LoadProductsAsync()
    {
        using var db = new AppDbContext();
        var q = db.Products.Include(p => p.Category).Where(p => p.IsActive);

        var s = SearchText?.Trim();
        if (!string.IsNullOrEmpty(s))
            q = q.Where(p => p.Name.Contains(s) || p.Sku.Contains(s)
                          || (p.Barcode != null && p.Barcode.Contains(s)));

        var list = await q.OrderBy(p => p.Name).ToListAsync();

        var stocks = await db.StockMovements
            .GroupBy(m => m.ProductId)
            .Select(g => new { Id = g.Key, Qty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.Id, x => x.Qty);

        Products.Clear();
        foreach (var p in list)
        {
            p.Stock = stocks.GetValueOrDefault(p.Id);
            Products.Add(p);
        }
    }

    public Task RefreshAsync() => LoadProductsAsync();

    [RelayCommand]
    private async Task ScanAsync()
    {
        var code = ScanText.Trim();
        ScanText = "";
        if (code == "") return;

        using var db = new AppDbContext();
        var upper = code.ToUpper();
        var found = await db.Products.FirstOrDefaultAsync(
            p => p.IsActive && (p.Barcode == code || p.Sku.ToUpper() == upper));

        SelectedProduct = null;
        if (found != null)
        {
            Editing = Copy(found);
            Message = $"Found: {found.Name}";
        }
        else
        {
            Editing = new Product
            {
                CategoryId = Categories.FirstOrDefault()?.Id ?? 0,
                Barcode = code,
                Sku = code
            };
            Message = "New code. Enter the name and prices, then Save.";
            NewCodeScanned?.Invoke();
        }
    }

    [RelayCommand]
    private void NewProduct()
    {
        SelectedProduct = null;
        Editing = new Product { CategoryId = Categories.FirstOrDefault()?.Id ?? 0 };
        Message = "";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var p = Editing;
        if (string.IsNullOrWhiteSpace(p.Sku) || string.IsNullOrWhiteSpace(p.Name) || p.CategoryId == 0)
        {
            Message = "SKU, Name and Category are required.";
            return;
        }

        try
        {
            using var db = new AppDbContext();
            var entity = new Product
            {
                Id = p.Id,
                Sku = p.Sku.Trim(),
                Barcode = string.IsNullOrWhiteSpace(p.Barcode) ? null : p.Barcode.Trim(),
                Name = p.Name.Trim(),
                CategoryId = p.CategoryId,
                SupplierId = p.SupplierId is null or 0 ? null : p.SupplierId,
                Unit = string.IsNullOrWhiteSpace(p.Unit) ? "pcs" : p.Unit.Trim(),
                CostPrice = p.CostPrice,
                RetailPrice = p.RetailPrice,
                WholesalePrice = p.WholesalePrice,
                ReorderLevel = p.ReorderLevel,
                IsActive = true
            };

            if (entity.Id == 0) db.Products.Add(entity);
            else db.Products.Update(entity);

            await db.SaveChangesAsync();
            await LoadProductsAsync();
            SelectedProduct = Products.FirstOrDefault(x => x.Id == entity.Id);
            Message = "Saved.";
        }
        catch (DbUpdateException)
        {
            Message = "Save failed. The SKU may already exist.";
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (Editing.Id == 0) return;
        if (MessageBox.Show($"Remove '{Editing.Name}'?", "Confirm",
                MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;

        using var db = new AppDbContext();
        var entity = await db.Products.FindAsync(Editing.Id);
        if (entity != null)
        {
            entity.IsActive = false;   // soft delete keeps movement history intact
            await db.SaveChangesAsync();
        }

        await LoadProductsAsync();
        NewProduct();
        Message = "Removed.";
    }
}