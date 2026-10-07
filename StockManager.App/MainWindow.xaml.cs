using System.Windows;
using StockManager.App.ViewModels;
using StockManager.Data;

namespace StockManager.App;

public partial class MainWindow : Window
{
    private readonly ProductsViewModel _vm = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;
        Loaded += async (_, _) =>
        {
            await _vm.InitAsync();
            try { await BackupService.BackupAsync(onlyIfNoneToday: true); } catch { }
        };
    }

    private async void OpenReceipt_Click(object sender, RoutedEventArgs e)
    {
        new ReceiptWindow { Owner = this }.ShowDialog();
        await _vm.RefreshAsync();
    }

    private async void OpenSale_Click(object sender, RoutedEventArgs e)
    {
        new SaleWindow { Owner = this }.ShowDialog();
        await _vm.RefreshAsync();
    }

    private async void OpenAdjust_Click(object sender, RoutedEventArgs e)
    {
        new AdjustmentWindow { Owner = this }.ShowDialog();
        await _vm.RefreshAsync();
    }

    private void OpenLowStock_Click(object sender, RoutedEventArgs e)
    {
        new LowStockWindow { Owner = this }.ShowDialog();
    }

    private async void Backup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var file = await BackupService.BackupAsync(onlyIfNoneToday: false);
            MessageBox.Show($"Backup saved:\n{file}", "Backup");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Backup failed: " + ex.Message, "Backup");
        }
    }

    private void OpenReports_Click(object sender, RoutedEventArgs e)
    {
        new ReportsWindow { Owner = this }.ShowDialog();
    }
}