#if FANTASY_NET
using System;
using System.Collections.Generic;
using System.IO;
// ReSharper disable SuspiciousTypeConversion.Global

namespace Fantasy
{
    /// <summary>
    /// 配置表帮助类
    /// </summary>
    public static class ConfigTableHelper
    {
        private static readonly object Lock = new object();
        // 配置表数据缓存字典
        private static readonly Dictionary<string, ASerialize> ConfigDic = new ();
        /// <summary>
        /// 加载配置表数据
        /// </summary>
        /// <typeparam name="T">配置表类型</typeparam>
        /// <returns>配置表数据</returns>
        public static T Load<T>() where T : ASerialize
        {
            lock (Lock)
            {
                try
                {
                    var dataConfig = typeof(T).Name;

                    if (ConfigDic.TryGetValue(dataConfig, out var aProto))
                    {
                        return (T)aProto;
                    }

                    var configFile = GetConfigPath(dataConfig);
                    var bytes = File.ReadAllBytes(configFile);
                    var data = MessagePackHelper.Deserialize<T>(bytes, 0, bytes.Length);
                    ConfigDic[dataConfig] = data;
                    return data;
                }
                catch (Exception ex)
                {
                    throw new Exception($"ConfigTableManage:{typeof(T).Name} 数据表加载之后反序列化时出错:{ex}");
                }
            }
        }

        /// <summary>
        /// 获取配置表文件路径
        /// </summary>
        /// <param name="name">配置表名称</param>
        /// <returns>配置表文件路径</returns>
        private static string GetConfigPath(string name)
        {
            var configFile = Path.Combine(ProcessDefine.ConfigTableBinaryDirectory, $"{name}.bytes");

            if (File.Exists(configFile))
            {
                return configFile;
            }

            throw new FileNotFoundException($"{name}.byte not found: {configFile}");
        }
        
        /// <summary>
        /// 重新加载配置表数据
        /// </summary>
        public static void ReLoadConfigTable()
        {
            lock (Lock)
            {
                foreach (var (_, aProto) in ConfigDic)
                {
                    ((IDisposable) aProto).Dispose();
                }

                ConfigDic.Clear();
            }
        }
    }
}
#endif
