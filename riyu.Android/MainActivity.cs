using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Avalonia;
using Avalonia.Android;
using Microsoft.Extensions.DependencyInjection;
using riyu.Services.Keyboard;
using riyu.Android.Keyboard;

namespace riyu.Android;

[Activity(
    Label = "日语单词斩",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    public static Activity? CurrentActivity { get; private set; }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        CurrentActivity = this;
        
        // 智能适配：根据系统类型选择不同的透明实现方式
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
        {
            // 设置窗口绘制系统栏背景
            Window?.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
            
            if (IsMIUI())
            {
                // MIUI系统：使用沉浸式虚拟键实现透明（参考小米开发文档）
                Window?.AddFlags(WindowManagerFlags.TranslucentStatus);       // 设置沉浸式状态栏
                Window?.AddFlags(WindowManagerFlags.TranslucentNavigation);   // 设置沉浸式虚拟键
            }
            else
            {
                // 原生Android系统（如Pixel 5）：使用SetNavigationBarColor实现透明
                Window?.SetNavigationBarColor(global::Android.Graphics.Color.ParseColor("#fffaf7"));
                Window?.SetStatusBarColor(global::Android.Graphics.Color.Transparent);
            }
        }
        
        // // Android 11+ 的新API，设置内容延伸到系统栏区域
        // if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
        // {
        //     Window?.SetDecorFitsSystemWindows(false);
        // }
    }

    protected override void OnResume()
    {
        base.OnResume();
        CurrentActivity = this;
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont()
            .AfterSetup(_ =>
            {
                // 简单服务定位器注册
                ServiceLocator.Register<IKeyboardService>(new AndroidKeyboardService());
            });
    }
    
    /// <summary>
    /// 检测是否为MIUI系统（小米系统）
    /// </summary>
    /// <returns>true表示是MIUI系统，false表示是原生Android系统</returns>
    private bool IsMIUI()
    {
        try
        {
            // 方法1：检查制造商信息（最可靠的方法）
            var manufacturer = global::Android.OS.Build.Manufacturer?.ToLower();
            if (!string.IsNullOrEmpty(manufacturer) && manufacturer.Contains("xiaomi"))
                return true;
                
            // 方法2：检查品牌信息
            var brand = global::Android.OS.Build.Brand?.ToLower();
            if (!string.IsNullOrEmpty(brand) && brand.Contains("xiaomi"))
                return true;
                
            // 方法3：检查产品信息
            var product = global::Android.OS.Build.Product?.ToLower();
            if (!string.IsNullOrEmpty(product) && product.Contains("mi"))
                return true;
                
            return false;
        }
        catch
        {
            // 如果检测失败，默认使用原生Android的方式
            return false;
        }
    }
}