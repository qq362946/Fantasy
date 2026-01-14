using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Fantasy.ProtocolEditor.Models;

namespace Fantasy.ProtocolEditor.Converters;

/// <summary>
/// EditorType 到 Boolean 的转换器，用于控制编辑器可见性
/// </summary>
public class EditorTypeToTextEditorVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is EditorType editorType)
        {
            return editorType == EditorType.TextEditor;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// EditorType 到 Boolean 的转换器，用于控制配置编辑器可见性
/// </summary>
public class EditorTypeToConfigEditorVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is EditorType editorType)
        {
            return editorType == EditorType.ConfigEditor;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 文件路径到 Boolean 的转换器，用于判断是否是 RoamingType.Config
/// </summary>
public class FilePathToRoamingConfigVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string filePath && !string.IsNullOrEmpty(filePath))
        {
            var fileName = Path.GetFileName(filePath);
            return fileName == "RoamingType.Config";
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 文件路径到 Boolean 的转换器，用于判断是否是 RouteType.Config
/// </summary>
public class FilePathToRouteConfigVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string filePath && !string.IsNullOrEmpty(filePath))
        {
            var fileName = Path.GetFileName(filePath);
            return fileName == "RouteType.Config";
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Boolean 到 Color 的转换器
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public string TrueColor { get; set; } = "#CCCCCC";
    public string FalseColor { get; set; } = "#808080";

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            var colorString = boolValue ? TrueColor : FalseColor;
            return Color.Parse(colorString);
        }
        return Color.Parse(FalseColor);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Null 到 Boolean 的转换器，用于控制欢迎界面可见性
/// 当 ActiveTab 为 null 时返回 true，显示欢迎界面
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value == null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Boolean 到验证背景色的转换器（VSCode Dark Theme）
/// True 时显示成功背景色，False 时显示错误背景色
/// </summary>
public class BoolToValidationBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isValid)
        {
            // 成功：深绿色背景（暗色调，与 VSCode 主题一致）
            // 错误：深红色背景（暗色调，与 VSCode 主题一致）
            return new SolidColorBrush(isValid ? Color.Parse("#1A3A1A") : Color.Parse("#3A1A1A"));
        }
        return new SolidColorBrush(Color.Parse("#2D2D30"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Boolean 到验证边框色的转换器（VSCode Dark Theme）
/// True 时显示成功边框色，False 时显示错误边框色
/// </summary>
public class BoolToValidationBorderConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isValid)
        {
            // 成功：柔和的绿色边框
            // 错误：柔和的红色边框
            return new SolidColorBrush(isValid ? Color.Parse("#4CAF50") : Color.Parse("#E57373"));
        }
        return new SolidColorBrush(Color.Parse("#3E3E42"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// EditorType 到 Boolean 的转换器，用于控制导出设置编辑器可见性
/// </summary>
public class EditorTypeToExportSettingsVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is EditorType editorType)
        {
            return editorType == EditorType.ExportSettings;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
