using Android.Content;
using Android.Views;
using Android.Views.InputMethods;
using riyu.Services.Keyboard;
using riyu.Android;

namespace riyu.Android.Keyboard;

public class AndroidKeyboardService : IKeyboardService
{
    public void Show()
    {
        var activity = MainActivity.CurrentActivity;
        if (activity == null)
            return;

        var inputMethodManager = (InputMethodManager?)activity.GetSystemService(Context.InputMethodService);
        if (inputMethodManager == null)
            return;

        var currentFocus = activity.CurrentFocus;
        if (currentFocus == null)
        {
            // 如果当前没有焦点视图，创建一个临时视图获取窗口Token
            currentFocus = new global::Android.Views.View(activity);
        }

        currentFocus.Post(() =>
        {
            inputMethodManager.ShowSoftInput(currentFocus, ShowFlags.Implicit);
        });
    }
}


