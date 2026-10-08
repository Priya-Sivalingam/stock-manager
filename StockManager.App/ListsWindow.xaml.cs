using System.Windows;
using StockManager.App.ViewModels;

namespace StockManager.App;

public partial class ListsWindow : Window
{
    public ListsWindow()
    {
        InitializeComponent();
        var vm = new ListsViewModel();
        DataContext = vm;
        Loaded += async (_, _) => await vm.InitAsync();
    }
}