using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using StockManager.Core;
using StockManager.Data;

namespace StockManager.App.ViewModels;

public partial class ListsViewModel : ObservableObject
{
    public ObservableCollection<Category> Categories { get; } = new();
    public ObservableCollection<Supplier> Suppliers { get; } = new();

    [ObservableProperty] private Category? selectedCategory;
    [ObservableProperty] private string categoryName = "";
    [ObservableProperty] private string categoryMessage = "";

    [ObservableProperty] private Supplier? selectedSupplier;
    [ObservableProperty] private string supplierName = "";
    [ObservableProperty] private string supplierPhone = "";
    [ObservableProperty] private string supplierMessage = "";

    partial void OnSelectedCategoryChanged(Category? value) => CategoryName = value?.Name ?? "";

    partial void OnSelectedSupplierChanged(Supplier? value)
    {
        SupplierName = value?.Name ?? "";
        SupplierPhone = value?.Phone ?? "";
    }

    public async Task InitAsync()
    {
        using var db = new AppDbContext();
        Categories.Clear();
        foreach (var c in await db.Categories.OrderBy(c => c.Name).ToListAsync()) Categories.Add(c);
        Suppliers.Clear();
        foreach (var s in await db.Suppliers.OrderBy(s => s.Name).ToListAsync()) Suppliers.Add(s);
    }

    // ---------- Categories ----------
    [RelayCommand]
    private void NewCategory()
    {
        SelectedCategory = null;
        CategoryName = "";
        CategoryMessage = "";
    }

    [RelayCommand]
    private async Task SaveCategoryAsync()
    {
        var name = CategoryName.Trim();
        if (name == "") { CategoryMessage = "Enter a name."; return; }

        var id = SelectedCategory?.Id ?? 0;
        var lower = name.ToLower();

        using var db = new AppDbContext();
        if (await db.Categories.AnyAsync(c => c.Name.ToLower() == lower && c.Id != id))
        {
            CategoryMessage = "That category already exists.";
            return;
        }

        if (id == 0) db.Categories.Add(new Category { Name = name });
        else
        {
            var c = await db.Categories.FindAsync(id);
            if (c != null) c.Name = name;
        }
        await db.SaveChangesAsync();

        await InitAsync();
        NewCategory();
        CategoryMessage = "Saved.";
    }

    [RelayCommand]
    private async Task DeleteCategoryAsync()
    {
        if (SelectedCategory == null) return;
        var id = SelectedCategory.Id;

        using var db = new AppDbContext();
        if (await db.Products.AnyAsync(p => p.CategoryId == id))
        {
            CategoryMessage = "Products use this category. Move them first.";
            return;
        }

        var c = await db.Categories.FindAsync(id);
        if (c != null) { db.Categories.Remove(c); await db.SaveChangesAsync(); }

        await InitAsync();
        NewCategory();
        CategoryMessage = "Deleted.";
    }

    // ---------- Suppliers ----------
    [RelayCommand]
    private void NewSupplier()
    {
        SelectedSupplier = null;
        SupplierName = "";
        SupplierPhone = "";
        SupplierMessage = "";
    }

    [RelayCommand]
    private async Task SaveSupplierAsync()
    {
        var name = SupplierName.Trim();
        if (name == "") { SupplierMessage = "Enter a name."; return; }

        var phone = string.IsNullOrWhiteSpace(SupplierPhone) ? null : SupplierPhone.Trim();
        var id = SelectedSupplier?.Id ?? 0;
        var lower = name.ToLower();

        using var db = new AppDbContext();
        if (await db.Suppliers.AnyAsync(s => s.Name.ToLower() == lower && s.Id != id))
        {
            SupplierMessage = "That supplier already exists.";
            return;
        }

        if (id == 0) db.Suppliers.Add(new Supplier { Name = name, Phone = phone });
        else
        {
            var s = await db.Suppliers.FindAsync(id);
            if (s != null) { s.Name = name; s.Phone = phone; }
        }
        await db.SaveChangesAsync();

        await InitAsync();
        NewSupplier();
        SupplierMessage = "Saved.";
    }

    [RelayCommand]
    private async Task DeleteSupplierAsync()
    {
        if (SelectedSupplier == null) return;
        var id = SelectedSupplier.Id;

        using var db = new AppDbContext();
        if (await db.Products.AnyAsync(p => p.SupplierId == id))
        {
            SupplierMessage = "Products use this supplier. Change them first.";
            return;
        }

        var s = await db.Suppliers.FindAsync(id);
        if (s != null) { db.Suppliers.Remove(s); await db.SaveChangesAsync(); }

        await InitAsync();
        NewSupplier();
        SupplierMessage = "Deleted.";
    }
}