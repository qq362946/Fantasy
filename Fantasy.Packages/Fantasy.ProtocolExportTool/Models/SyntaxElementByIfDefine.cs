using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fantasy.ProtocolExportTool.Models;

/// <summary>
/// 自定义语法元素字典, 根据条件编译符分组。
/// Key为条件编译符，Value为语法元素。
/// </summary>
public sealed class SyntaxElementByIfDefine : Dictionary<string, List<string>>
{
    /// <summary>
    /// 将另一个自定义语法元素字典合并到当前自定义语法元素字典（会自动去重处理）
    /// </summary>
    public SyntaxElementByIfDefine Merge(SyntaxElementByIfDefine other)
    {
        if (other == null) return this;

        foreach (var kv in other)
        {
            if (this.TryGetValue(kv.Key, out var existingList))
            {
                // key 已存在，追加元素
                existingList.AddRange(kv.Value.Except(existingList));
            }
            else
            {
                // key 不存在，直接克隆一份
                this[kv.Key] = new (kv.Value);
            }
        }
        return this;
    }

    /// <summary>
    /// 克隆自定义语法元素
    /// </summary>
    public SyntaxElementByIfDefine CloneOne()
    {
        var clone = new SyntaxElementByIfDefine();
        foreach (var kv in this)
        {
            clone[kv.Key] = new(kv.Value);
        }
        foreach (var kv in this)
        {
            clone[kv.Key] = new(kv.Value);
        }
        return clone;
    }

    /// <summary>
    /// 将自身转为代码字符串
    /// </summary>
    /// <param name="customNamespace"></param>
    /// <returns></returns>
    public string ToCodeString()
    {
        var builder = new StringBuilder();
        foreach (var kv in this)
        {
            bool ifAnyCondition = !string.IsNullOrWhiteSpace(kv.Key); // 是否有条件编译符

            if (ifAnyCondition)
                builder.AppendLine($"#if {kv.Key}");

            foreach (var nameSpace in kv.Value)
            {
                builder.AppendLine($"using {nameSpace.Replace(";", "")};");
            }

            if (ifAnyCondition)
                builder.AppendLine($"#endif");
        }
        return builder.ToString();
    }
}
