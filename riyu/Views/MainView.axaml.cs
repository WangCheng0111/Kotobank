using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Input;
using Avalonia.Styling;
using System;
using System.Linq;
using System.Threading.Tasks;
using riyu.Services.Keyboard;

namespace riyu.Views;

public partial class MainView : UserControl
{
    private DispatcherTimer? _spinnerTimer;
    private double _currentAngle = 0;
    private Button? _confirmButton;
    private TextBox? _japaneseTextBox;
    
    public MainView()
    {
        InitializeComponent();
        InitializeSpinner();
        HookConfirmWithoutLosingFocus();
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
                if (sender is TextBox tb)
                {
                    viewModel.ConfirmAnswerCommand.Execute(tb.Text);
                }
                else
                {
                    viewModel.ConfirmAnswerCommand.Execute(_japaneseTextBox?.Text);
                }
            }
            
            // 阻止事件继续传播
            e.Handled = true;
        }
    }

    private void HookConfirmWithoutLosingFocus()
    {
        _confirmButton = this.FindControl<Button>("ConfirmButton");
        _japaneseTextBox = this.FindControl<TextBox>("JapaneseTextBox");

        if (_confirmButton != null)
        {
            _confirmButton.AddHandler(InputElement.PointerPressedEvent, OnConfirmPointerPressed, RoutingStrategies.Tunnel);
            _confirmButton.AddHandler(InputElement.PointerReleasedEvent, OnConfirmPointerReleased, RoutingStrategies.Bubble);
            _confirmButton.AddHandler(InputElement.PointerCaptureLostEvent, OnConfirmPointerCaptureLost, RoutingStrategies.Bubble);
        }

        if (_japaneseTextBox != null)
        {
            _japaneseTextBox.AddHandler(InputElement.PointerPressedEvent, OnTextBoxPointerPressed, RoutingStrategies.Tunnel);
        }
    }

    private void OnConfirmPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // 隧道阶段拦截，防止 FocusManager 抢焦点
        e.Handled = true;

        // 手动设置按下类以恢复按下视觉（通过样式匹配）
        _confirmButton?.Classes.Add("manual-pressed");
        // 不在 PointerPressed 执行命令；等待 PointerReleased 再执行，确保 Android IME 已提交
    }

    private void OnConfirmPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _confirmButton?.Classes.Remove("manual-pressed");
        // 在释放阶段执行命令，传入当前文本（避免 Android 上 IME 未提交问题）
        if (DataContext is ViewModels.MainViewModel viewModel)
        {
            var parameter = _japaneseTextBox?.Text;
            if (viewModel.ConfirmAnswerCommand.CanExecute(parameter))
            {
                viewModel.ConfirmAnswerCommand.Execute(parameter);
            }
        }
        // 释放时保证焦点仍在文本框
        _japaneseTextBox?.Focus();
    }

    private void OnConfirmPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        _confirmButton?.Classes.Remove("manual-pressed");
    }

    private void OnTextBoxPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // 在 Android 上，如果文本框已有焦点但系统软键盘被手动隐藏，则再次点击时强制弹出键盘
        if (_japaneseTextBox?.IsFocused == true)
        {
            ServiceLocator.Resolve<IKeyboardService>()?.Show();
        }
    }
}