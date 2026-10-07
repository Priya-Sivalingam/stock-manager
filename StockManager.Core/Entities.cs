namespace StockManager.Core;
using System.ComponentModel.DataAnnotations.Schema;

public class Category { public int Id { get; set; } public string Name { get; set; } = ""; }

public class Supplier
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
}

public class Product
{
    public int Id { get; set; }
    public string Sku { get; set; } = "";
    public string? Barcode { get; set; }
    public string Name { get; set; } = "";
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public string Unit { get; set; } = "pcs";
    public decimal CostPrice { get; set; }
    public decimal RetailPrice { get; set; }
    public decimal WholesalePrice { get; set; }
    public int ReorderLevel { get; set; }
    public bool IsActive { get; set; } = true;

    [NotMapped] public int Stock { get; set; }
    [NotMapped] public bool IsLow => Stock <= ReorderLevel;
}

public enum MovementType { Receipt, Sale, Adjustment, Return }

public class StockMovement
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public MovementType Type { get; set; }
    public int Quantity { get; set; }          // + in, - out
    public decimal UnitPrice { get; set; }
    public string? Reason { get; set; }
    public string? Reference { get; set; }     // invoice / PO no.
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "";
}