using System.Windows;
using StockManager.App.ViewModels;

namespace StockManager.App;

public partial class SaleWindow : Window
{
    public SaleWindow()
    {
        InitializeComponent();
        var vm = new SaleViewModel();
        DataContext = vm;
        Loaded += (_, _) => ScanBox.Focus();
        vm.Cart.CollectionChanged += (_, _) => ScanBox.Focus();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SaleViewModel.Message)) ScanBox.Focus();
        };
    }
}