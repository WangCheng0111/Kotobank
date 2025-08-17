using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Input;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace riyu.Views;

public partial class MainView : UserControl
{
    private DispatcherTimer? _spinnerTimer;
    private double _currentAngle = 0;
    
    public MainView()
    {
        InitializeComponent();
        InitializeSpinner();
    }
    
    private void InitializeSpinner()
    {
        // 创建定时器，每50毫秒旋转6度
        _spinnerTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        
        _spinnerTimer.Tick += (sender, e) =>
        {
            _currentAngle += 6; // 每次旋转6度
            if (_currentAngle >= 360)
                _currentAngle = 0;
                
            // 更新旋转角度 - 查找所有PathIcon控件
            var pathIcons = this.GetVisualDescendants().OfType<PathIcon>();
            foreach (var icon in pathIcons)
            {
                if (icon.RenderTransform is RotateTransform rotateTransform)
                {
                    rotateTransform.Angle = _currentAngle;
                }
            }
        };
        
        // 启动定时器
        _spinnerTimer.Start();
    }
    
    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        _spinnerTimer?.Stop();
    }
    
    // 处理日语输入框的KeyDown事件
    private void OnJapaneseInputKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            // 如果按下回车键，调用ViewModel的确认答案命令
            if (DataContext is ViewModels.MainViewModel viewModel)
            {
                viewModel.ConfirmAnswerCommand.Execute(null);
            }
            
            // 阻止事件继续传播
            e.Handled = true;
        }
    }
}