using SwiftGrab.ViewModels;

namespace SwiftGrab.Views;

public partial class MainWindow : MahApps.Metro.Controls.MetroWindow
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
