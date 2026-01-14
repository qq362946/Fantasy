using CommunityToolkit.Mvvm.ComponentModel;

namespace Fantasy.ProtocolEditor.Models;

/// <summary>
/// 表示 RoamingType.Config 文件中的一个条目
/// </summary>
public partial class RoamingTypeEntry : ObservableObject
{
    /// <summary>
    /// Roaming 类型名称 (例如: MapRoamingType)
    /// </summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// Roaming 类型的值 (必须 >= 10000)
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
        return !string.IsNullOrWhiteSpace(Name) && Value >= 10000;
    }

    /// <summary>
    /// 获取错误信息
    /// </summary>
    public string? GetErrorMessage()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return "名称不能为空";

        if (!Name.EndsWith("RoamingType"))
            return "名称必须以 RoamingType 结尾";

        if (Value < 10000)
            return "值必须大于等于 10000 (10000以内框架预留)";

        return null;
    }
}
