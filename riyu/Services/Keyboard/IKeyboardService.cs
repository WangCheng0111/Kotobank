namespace riyu.Services.Keyboard;

/// <summary>
/// 平台软键盘服务接口。
/// </summary>
public interface IKeyboardService
{
    /// <summary>
    /// 在当前平台尝试显示软键盘（如已显示则无副作用）。
    /// </summary>
    void Show();
}


