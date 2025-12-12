using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fantasy.ProtocolExportTool.Models
{
    /// <summary>
    /// 根据条件编译符分组的字典抽象类。
    /// 有 <see cref="CustomAttributesByIfDefine"/>、
    /// <see cref="MessagesByIfDefine"/>、
    /// <see cref="PropertyByIfDefine"/>等子类具体实现。
    /// </summary>
    /// <typeparam name="T">要分组的类型</typeparam>
    public abstract class IfDefineDictionary<T> : Dictionary<string, List<T>>
    {       
        /// <summary>
        /// 转换为C#代码
        /// </summary>
        public string ToCSharpLines(object? arg = null) {
           return ToCSharpStringBuilder(arg).ToString();
        }

        /// <summary>
        /// 必须由子类分别实现： 将列表元素写出字符串, 返回的字符串将会自动写在条件编译符的中间部分。
        /// </summary>
        public abstract string WriteCSharpLine(T listElement, object? arg = null);

        /// <summary>
        /// 转换为C#代码字符串构建器
        /// </summary>
        public StringBuilder ToCSharpStringBuilder(object? arg = null) {
            var builder = new StringBuilder();
            foreach (var kv in this)
            {
                bool ifAnyCondition = !string.IsNullOrWhiteSpace(kv.Key); // 是否有条件编译符

                if (ifAnyCondition)
                    builder.AppendLine($"#if {kv.Key}");

                foreach (T listElement in kv.Value)
                {
                    builder.AppendLine(WriteCSharpLine(listElement,arg));
                }

                if (ifAnyCondition)
                    builder.AppendLine($"#endif");
            }
            return builder;
        }

        /// <summary>
        /// 将另一个同类型字典合并到当前字典（会自动去重）,返回一个整体。
        /// </summary>
        public virtual IfDefineDictionary<T> Merge(IfDefineDictionary<T>? other)
        {
            if (other == null) return this;

            foreach (var kv in other)
            {
                if (this.TryGetValue(kv.Key, out var existingList))
                {
                    // 去重追加
                    existingList.AddRange(kv.Value.Except(existingList));
                }
                else
                {
                    // key 不存在，深克隆 value
                    this[kv.Key] = new List<T>(kv.Value);
                }
            }
            return this;
        }

        /// <summary>
        /// 克隆整个字典（深克隆 List）
        /// </summary>
        public virtual IfDefineDictionary<T> CloneOne()
        {
            var clone = (IfDefineDictionary<T>)Activator.CreateInstance(this.GetType())!;

            foreach (var kv in this)
            {
                clone[kv.Key] = new List<T>(kv.Value);
            }

            return clone;
        }
    }
}
