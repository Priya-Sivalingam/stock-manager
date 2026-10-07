using System.Windows;
using StockManager.App.ViewModels;

namespace StockManager.App;

public partial class AdjustmentWindow : Window
{
    public AdjustmentWindow()
    {
        InitializeComponent();
        var vm = new AdjustmentViewModel();
        DataContext = vm;
        Loaded += async (_, _) => await vm.InitAsync();
    }
}