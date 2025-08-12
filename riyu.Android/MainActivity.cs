using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia;
using Avalonia.Android;

namespace riyu.Android;

[Activity(
    Label = "日语单词斩",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        
        // 强制设置导航栏和状态栏颜色，解决MIUI等厂商系统的兼容性问题
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
        {
            // 设置导航栏颜色为应用背景色，注意这里设置的颜色要和主界面的背景色一致，否则会不生效
            Window?.SetNavigationBarColor(global::Android.Graphics.Color.ParseColor("#fffaf7"));
            // 设置状态栏为透明
            Window?.SetStatusBarColor(global::Android.Graphics.Color.Transparent);
        }
        
        // Android 11+ 的新API
        if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
        {
            Window?.SetDecorFitsSystemWindows(false);
        }
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }
}