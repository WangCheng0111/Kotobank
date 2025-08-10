using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Interactivity;

namespace riyu.Views;

public partial class AndroidMainView : UserControl
{
    public AndroidMainView()
    {
        InitializeComponent();
    }

    // protected override void OnLoaded(RoutedEventArgs e)
    // {
    //     base.OnLoaded(e);
    //     var insetsManager = TopLevel.GetTopLevel(this)?.InsetsManager;
    //
    //     if (insetsManager != null)
    //     {
    //         insetsManager.DisplayEdgeToEdgePreference = true;
    //     }
    // }
}