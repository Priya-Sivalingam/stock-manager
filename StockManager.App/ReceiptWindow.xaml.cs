using System.Windows;
using StockManager.App.ViewModels;

namespace StockManager.App;

public partial class ReceiptWindow : Window
{
    public ReceiptWindow()
    {
        InitializeComponent();
        var vm = new ReceiptViewModel();
        DataContext = vm;
        Loaded += async (_, _) => await vm.InitAsync();
    }
}