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
    internal static class FTableHelper
    {
        /// <summary>
        /// 扫描程序集中所有含有[Table]或[FTable]标签的类型, 并进行操作
        /// <param name="doSomethingWithFTableAndTableName"> 拿到FTable的类型和表名, 针对性地进行操作。</param>
        /// </summary>       
        public static void ScanFTableTypes(Action<Type,string> doSomethingWithFTableAndTableName) {

            foreach (var kv in AssemblyManifest.Manifests)
            {
                var assm = kv.Value.Assembly;

                //Log.Info($"Scanning for FTables in assembly: {assm.FullName}");

                var FTables = assm.GetTypes()
                  .Where(t => t.IsClass && !t.IsAbstract)
                  .Where(t => t.GetCustomAttributes(typeof(TableAttribute), true).Any());

                foreach (var type in FTables)
                {
                    // 优先检查 FantasyTableAttribute.Name，其次检查 TableAttribute.Name
                    string? tableName = null;
                    var fantasyAttr = type.GetCustomAttribute<FTableAttribute>();
                    if (fantasyAttr != null && !string.IsNullOrWhiteSpace(fantasyAttr.Name))
                        tableName = fantasyAttr.Name;
                    else
                    {
                        var tableAttr = type.GetCustomAttribute<TableAttribute>();
                        if (tableAttr != null && !string.IsNullOrWhiteSpace(tableAttr.Name))
                            tableName = tableAttr.Name;
                    }

                    // 没有用标签设置自定义表名, 那就直接用类名作为表名
                    tableName ??= $"{type.Name}";

                    doSomethingWithFTableAndTableName.Invoke(type, tableName);
                }
            }
        }
        /// <summary>
        /// 异步版本,传入异步函数
        /// </summary>
        public static async FTask ScanFTableTypesAsync(Func<Type, string, FTask> doSomethingWithFTableAndTableName)
        {
            foreach (var kv in AssemblyManifest.Manifests)
            {
                var assm = kv.Value.Assembly;

                Log.Info($"Scanning assembly: {assm.FullName}");

                var FTables = assm.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract)
                    .Where(t => t.GetCustomAttributes(typeof(TableAttribute), true).Any());

                foreach (var type in FTables)
                {
                    string? tableName = null;
                    var fantasyAttr = type.GetCustomAttribute<FTableAttribute>();
                    if (fantasyAttr != null && !string.IsNullOrWhiteSpace(fantasyAttr.Name))
                        tableName = fantasyAttr.Name;
                    else
                    {
                        var tableAttr = type.GetCustomAttribute<TableAttribute>();
                        if (tableAttr != null && !string.IsNullOrWhiteSpace(tableAttr.Name))
                            tableName = tableAttr.Name;
                    }

                    tableName ??= type.Name;

                    // await 调用异步方法
                    await doSomethingWithFTableAndTableName(type, tableName);
                }
            }
        }
    }
}
