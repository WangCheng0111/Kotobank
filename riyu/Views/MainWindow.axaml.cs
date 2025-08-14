using Avalonia.Controls;
using Avalonia;
using riyu.ViewModels;

namespace riyu.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // 创建ViewModel并设置DataContext
        var viewModel = new MainViewModel();
        DataContext = viewModel;
        
        // 将窗口引用传递给ViewModel
        viewModel.SetWindowReference(this);
        
        // 监听窗口状态变化
        PropertyChanged += MainWindow_PropertyChanged;
    }
    
    private void MainWindow_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == WindowStateProperty)
        {
            // 更新ViewModel中的窗口边距和按钮图标
            if (DataContext is MainViewModel viewModel)
            {
                var isMaximized = WindowState == WindowState.Maximized;
                viewModel.UpdateWindowPadding(isMaximized);
                viewModel.UpdateMaximizeButtonIcon(isMaximized);
            }
        }
    }
}