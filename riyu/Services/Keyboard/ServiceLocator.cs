using System;
using System.Collections.Concurrent;

namespace riyu.Services.Keyboard;

/// <summary>
/// 极简服务定位器（仅用于示例中注入跨平台服务）。
/// </summary>
public static class ServiceLocator
{
    private static readonly ConcurrentDictionary<Type, object> _services = new();

    public static void Register<T>(T instance) where T : class
    {
        _services[typeof(T)] = instance;
    }

    public static T? Resolve<T>() where T : class
    {
        if (_services.TryGetValue(typeof(T), out var value))
        {
            return value as T;
        }
        return null;
    }
}


