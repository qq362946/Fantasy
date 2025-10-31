using Fantasy.Assembly;
using Fantasy.Async;
using Fantasy.Database.Attributes;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Fantasy.Database
{
    /// <summary>
    /// 数据标签元数据操作帮助类
    /// </summary>
    internal static class DbAttrHelper
    {
        /// <summary>
        /// 扫描程序集中所有含有[FantasyDbSet]标签的类型, 并进行操作。
        /// <param name="doSomething">  传入针对性的操作函数。</param>
        /// </summary>       
        public static void ScanFantasyDbSetTypes(Action<Type, string, FantasyDbSetAttribute> doSomething) {

            foreach (var kv in AssemblyManifest.Manifests)
            {
                var assm = kv.Value.Assembly;

                //Log.Info($"Scanning for FantasyDbSets in assembly: {assm.FullName}");

                foreach (var type in assm.GetTypes())
                {
                    var attr = type.GetCustomAttribute<FantasyDbSetAttribute>();

                    if (attr == null || type.IsAbstract)
                        continue;

                    // 检查标签中设置的 Name
                    string? tableName = default;
                    if (!string.IsNullOrWhiteSpace(attr.Name))
                        tableName = attr.Name;
                    else
                        tableName ??= $"{type.Name}"; // 没有用标签设置自定义表名, 那就直接用类名作为表名

                    doSomething.Invoke(type, tableName, attr);
                }
            }
        }
        /// <summary>
        /// 扫描程序集中所有含有[FantasyDbSet]标签的类型, 并进行操作。
        /// 异步版本,传入异步函数。
        /// </summary>
        public static async FTask ScanFantasyDbSetTypesAsync(Func<Type, string, FantasyDbSetAttribute, FTask> doSomething)
        {
            foreach (var kv in AssemblyManifest.Manifests)
            {
                var assm = kv.Value.Assembly;

                Log.Info($"Scanning assembly: {assm.FullName}");
                foreach (var type in assm.GetTypes())
                {
                    var attr = type.GetCustomAttribute<FantasyDbSetAttribute>();

                    if (attr == null || type.IsAbstract)
                        continue;

                    // 检查标签中设置的 Name
                    string? tableName = default;
                    if (!string.IsNullOrWhiteSpace(attr.Name))
                        tableName = attr.Name;
                    else
                        tableName ??= $"{type.Name}"; // 没有用标签设置自定义表名, 那就直接用类名作为表名

                    await doSomething.Invoke(type, tableName, attr);
                }
            }
        }
    }
}
