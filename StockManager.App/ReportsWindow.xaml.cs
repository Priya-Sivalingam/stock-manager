using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using StockManager.Core;
using StockManager.Data;

namespace StockManager.App;

public record StockRow(string Sku, string Name, int Stock, decimal Cost, decimal Value);
public record SalesRow(string Sku, string Name, int Qty, decimal Revenue, decimal Profit);

public partial class ReportsWindow : Window
{
    private List<StockRow> _stockRows = new();
    private List<SalesRow> _salesRows = new();

    public ReportsWindow()
    {
        InitializeComponent();
        FromDate.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        ToDate.SelectedDate = DateTime.Today;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private async Task LoadAsync()
    {
        var fromUtc = (FromDate.SelectedDate ?? DateTime.Today).Date.ToUniversalTime();
        var toUtc = (ToDate.SelectedDate ?? DateTime.Today).Date.AddDays(1).ToUniversalTime();

        using var db = new AppDbContext();

        var products = await db.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
        var stocks = await db.StockMovements
            .GroupBy(m => m.ProductId)
            .Select(g => new { Id = g.Key, Qty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.Id, x => x.Qty);

        _stockRows = products.Select(p =>
        {
            var s = stocks.GetValueOrDefault(p.Id);
            return new StockRow(p.Sku, p.Name, s, p.CostPrice, s * p.CostPrice);
        }).ToList();

        var sales = await db.StockMovements.Include(m => m.Product)
            .Where(m => m.Type == MovementType.Sale && m.CreatedAt >= fromUtc && m.CreatedAt < toUtc)
            .ToListAsync();

        _salesRows = sales.GroupBy(m => m.ProductId).Select(g =>
        {
            var p = g.First().Product!;
            var qty = -g.Sum(x => x.Quantity);
            var revenue = g.Sum(x => -x.Quantity * x.UnitPrice);
            return new SalesRow(p.Sku, p.Name, qty, revenue, revenue - qty * p.CostPrice);
        }).OrderByDescending(r => r.Revenue).ToList();

        StockGrid.ItemsSource = _stockRows;
        SalesGrid.ItemsSource = _salesRows;
        StockTotal.Text = $"Total stock value at cost: {_stockRows.Sum(r => r.Value):N2}";
        SalesTotal.Text = $"Revenue: {_salesRows.Sum(r => r.Revenue):N2}   |   Est. profit: {_salesRows.Sum(r => r.Profit):N2}";
    }

    private static string Q(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";
    private static string N(decimal d) => d.ToString("0.00", CultureInfo.InvariantCulture);

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        bool isStock = Tabs.SelectedIndex == 0;
        var dlg = new SaveFileDialog
        {
            Filter = "CSV file (*.csv)|*.csv",
            FileName = (isStock ? "stock-valuation-" : "sales-") + DateTime.Now.ToString("yyyyMMdd") + ".csv"
        };
        if (dlg.ShowDialog() != true) return;

        var sb = new StringBuilder();
        if (isStock)
        {
            sb.AppendLine("SKU,Name,Stock,Cost,Value");
            foreach (var r in _stockRows)
                sb.AppendLine($"{Q(r.Sku)},{Q(r.Name)},{r.Stock},{N(r.Cost)},{N(r.Value)}");
        }
        else
        {
            sb.AppendLine("SKU,Name,Qty sold,Revenue,Est. profit");
            foreach (var r in _salesRows)
                sb.AppendLine($"{Q(r.Sku)},{Q(r.Name)},{r.Qty},{N(r.Revenue)},{N(r.Profit)}");
        }

        File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));
        MessageBox.Show("Exported:\n" + dlg.FileName, "Export");
    }
}