using System.Windows;

namespace StockManager.App;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += (_, e) =>
        {
            MessageBox.Show("Something went wrong:\n" + e.Exception.Message, "Stock Manager");
            e.Handled = true;
        };
    }
}