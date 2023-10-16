using System.Reflection;
using System.Text;
using Fantasy;

namespace Exporter.Excel;

/// <summary>
/// 动态配置数据类型类，用于存储动态配置数据的相关信息。
/// </summary>
public class DynamicConfigDataType
{
    /// <summary>
    /// 配置数据对象，继承自 AProto 基类。
    /// </summary>
    public AProto ConfigData;
    /// <summary>
    /// 配置数据类型。
    /// </summary>
    public Type ConfigDataType;
    /// <summary>
    /// 配置类型。
    /// </summary>
    public Type ConfigType;
    /// <summary>
    /// 反射方法信息，用于调用特定方法。
    /// </summary>
    public MethodInfo Method;
    /// <summary>
    /// 配置数据对象实例。
    /// </summary>
    public object Obj;
    /// <summary>
    /// 用于生成 JSON 格式数据的字符串构建器。
    /// </summary>
    public StringBuilder Json = new StringBuilder();
}