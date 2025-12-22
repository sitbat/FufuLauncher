using Microsoft.Windows.ApplicationModel.Resources;

namespace FufuLauncher.Helpers;

public static class ResourceExtensions
{
    // 改用 ResourceManager，它在非打包应用中通常更稳定
    private static ResourceManager _resourceManager;

    public static string GetLocalized(this string resourceKey)
    {
        try
        {
            // 延迟初始化：确保在首次调用时才创建实例，而不是程序一启动就创建
            // 这样可以确保 Windows App SDK 已经完成引导 (Bootstrap)
            if (_resourceManager == null)
            {
                _resourceManager = new ResourceManager();
            }

            // 尝试获取资源
            var candidate = _resourceManager.MainResourceMap.GetValue(resourceKey);
            return candidate != null ? candidate.ValueAsString : resourceKey;
        }
        catch
        {
            // 如果发生任何错误（如资源文件丢失），直接返回 key，防止程序崩溃
            return resourceKey;
        }
    }
}