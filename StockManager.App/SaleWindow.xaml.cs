using System.Windows;
using StockManager.App.ViewModels;

namespace StockManager.App;

public partial class SaleWindow : Window
{
    public SaleWindow()
    {
        InitializeComponent();
        DataContext = new SaleViewModel();
        Loaded += (_, _) => ScanBox.Focus();
    }
}