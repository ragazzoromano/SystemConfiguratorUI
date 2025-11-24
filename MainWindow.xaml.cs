using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using SystemConfiguratorUI.ViewModels;

namespace SystemConfiguratorUI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.SelectedNode = e.NewValue as JsonTreeNodeViewModel;
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        if (DataContext is MainViewModel vm && !vm.ConfirmClose())
        {
            e.Cancel = true;
        }
    }
}
