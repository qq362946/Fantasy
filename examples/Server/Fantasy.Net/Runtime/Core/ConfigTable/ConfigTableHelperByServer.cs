#if FANTASY_NET
using System;
using System.Collections.Generic;
using System.IO;

namespace Fantasy
{
    /// <summary>
    /// 配置表帮助类
    /// </summary>
    public class ConfigTableHelper : Singleton<ConfigTableHelper>
    {
        private readonly object _lock = new object();
        // 配置表数据缓存字典
        private readonly Dictionary<string, AProto> ConfigDic = new ();

        /// <summary>
        /// 加载配置表数据
        /// </summary>
        /// <typeparam name="T">配置表类型</typeparam>
        /// <returns>配置表数据</returns>
        public T Load<T>() where T : AProto
        {
            lock (_lock)
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
                    var data = (T)ProtoBuffHelper.FromBytes(typeof(T), bytes, 0, bytes.Length);
                    data.AfterDeserialization();
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
        private string GetConfigPath(string name)
        {
            var configFile = Path.Combine(AppDefine.ConfigTableBinaryDirectory, $"{name}.bytes");

            if (File.Exists(configFile))
            {
                return configFile;
            }

            throw new FileNotFoundException($"{name}.byte not found: {configFile}");
        }
        
        /// <summary>
        /// 重新加载配置表数据
        /// </summary>
        public void ReLoadConfigTable()
        {
            lock (_lock)
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
