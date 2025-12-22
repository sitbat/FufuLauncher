using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using FufuLauncher.ViewModels; // 【关键】引用存放 WindowBackdropType 的命名空间

namespace FufuLauncher.Helpers;

public class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (parameter is string enumString && value != null)
        {
            // 情况 A: ViewModel 属性已经是枚举
            if (value.GetType().IsEnum)
            {
                try 
                {
                    var parsedValue = Enum.Parse(value.GetType(), enumString);
                    return parsedValue.Equals(value);
                }
                catch { return false; }
            }
            
            // 情况 B: ViewModel 属性是 int (旧代码或未修改成功)
            // 我们尝试把它当做 WindowBackdropType 来对比
            if (value is int intValue)
            {
                try
                {
                    // 把字符串 (如 "Mica") 转为 WindowBackdropType 枚举
                    var enumValue = Enum.Parse(typeof(WindowBackdropType), enumString);
                    // 把枚举转为 int 进行比较
                    return (int)enumValue == intValue;
                }
                catch { return false; }
            }
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        // 只有当 RadioButton 选中 (true) 时才处理
        if (value is bool isChecked && isChecked && parameter is string enumString)
        {
            try
            {
                // 分支 1: 如果目标本来就是枚举 (完美情况)
                if (targetType.IsEnum)
                {
                    return Enum.Parse(targetType, enumString);
                }

                // 分支 2: 如果目标是 int (您的现状)，由于报错说 targetType 不是 Enum
                // 我们手动强制解析为 WindowBackdropType，然后转回 int
                if (targetType == typeof(int) || targetType == typeof(object))
                {
                    var enumValue = Enum.Parse(typeof(WindowBackdropType), enumString);
                    return (int)enumValue; // 返回整数 0, 1, 2
                }
            }
            catch
            {
                return DependencyProperty.UnsetValue;
            }
        }
        return DependencyProperty.UnsetValue;
    }
}