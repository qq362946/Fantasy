using System;
using System.Collections.Generic;
using Fantasy.Serialize;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

// ReSharper disable SuspiciousTypeConversion.Global

namespace Fantasy.ConfigTable
{
    /// <summary>
    /// 配置表帮助类
    /// </summary>
    public static class ConfigTableHelper
    {
        private static string _assetBundleDirectoryPath;
        private static IConfigTableAssetBundle _configTableAssetBundle;
        private static readonly object Lock = new object();
        // 配置表数据缓存字典
        private static readonly Dictionary<string, ASerialize> ConfigDic = new ();

        /// <summary>
        /// 初始化ConfigTableHelper
        /// </summary>
        /// <param name="assetBundleDirectoryPath"></param>
        /// <param name="configTableAssetBundle"></param>
        public static void Initialize(string assetBundleDirectoryPath, IConfigTableAssetBundle configTableAssetBundle)
        {
            _assetBundleDirectoryPath = assetBundleDirectoryPath;
            _configTableAssetBundle = configTableAssetBundle;
        }
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
                    
                    var dataConfigPath = _configTableAssetBundle.Combine(_assetBundleDirectoryPath, dataConfig);
                    var bytes = _configTableAssetBundle.LoadConfigTable(dataConfigPath);
                    var data = SerializerManager.GetSerializer(FantasySerializerType.ProtoBuf).Deserialize<T>(bytes);
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
