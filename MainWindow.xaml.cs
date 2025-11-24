using System;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SystemConfiguratorUI.ViewModels;

namespace SystemConfiguratorUI;

public partial class MainWindow : Window
{
    private ScrollViewer? _rawJsonScrollViewer;

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

    private void RawJsonEditor_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateLineNumbers();
        SyncLineNumberScroll();
    }

    private void RawJsonEditor_Loaded(object sender, RoutedEventArgs e)
    {
        _rawJsonScrollViewer = FindDescendant<ScrollViewer>(RawJsonEditor);
        if (_rawJsonScrollViewer != null)
        {
            _rawJsonScrollViewer.ScrollChanged += RawJsonScrollViewer_ScrollChanged;
        }

        UpdateLineNumbers();
        SyncLineNumberScroll();
    }

    private void RawJsonScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        LineNumbersScrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
    }

    private void UpdateLineNumbers()
    {
        if (RawJsonEditor == null)
        {
            return;
        }

        var lineCount = Math.Max(RawJsonEditor.LineCount, 1);
        var builder = new StringBuilder();

        for (int i = 1; i <= lineCount; i++)
        {
            builder.Append(i).Append('\n');
        }

        LineNumbersTextBlock.Text = builder.ToString();
    }

    private void SyncLineNumberScroll()
    {
        if (_rawJsonScrollViewer != null)
        {
            LineNumbersScrollViewer.ScrollToVerticalOffset(_rawJsonScrollViewer.VerticalOffset);
        }
    }

    private static T? FindDescendant<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
            {
                return typedChild;
            }

            var nested = FindDescendant<T>(child);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }
}
