using CommunityToolkit.Mvvm.ComponentModel;

namespace Fantasy.ProtocolEditor.Models;

/// <summary>
/// 表示 RouteType.Config 文件中的一个条目
/// </summary>
public partial class RouteTypeEntry : ObservableObject
{
    /// <summary>
    /// Route 类型名称 (例如: PlayerRouteType)
    /// </summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// Route 类型的值 (必须 >= 1000)
    /// </summary>
    [ObservableProperty]
    private int _value;

    /// <summary>
    /// 是否正在编辑状态
    /// </summary>
    [ObservableProperty]
    private bool _isEditing;

    /// <summary>
    /// 验证条目是否有效
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Name) && Value >= 1000;
    }

    /// <summary>
    /// 获取错误信息
    /// </summary>
    public string? GetErrorMessage()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return "名称不能为空";

        if (!Name.EndsWith("RouteType"))
            return "名称必须以 RouteType 结尾";

        if (Value < 1000)
            return "值必须大于等于 1000 (1000以内框架预留)";

        return null;
    }
}
