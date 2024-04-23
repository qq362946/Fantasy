#if FANTASY_UNITY
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fantasy
{
    /// <summary>
    /// 配置表帮助类
    /// </summary>
    public class ConfigTableHelper : Singleton<ConfigTableHelper>
    {
        private readonly object _lock = new object();
        // 配置表数据缓存字典
        private static readonly string ConfigBundle = "Config".ToLower(); // 配置表资源包名称
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
                    
                    var bytes = Scene.Instance.AssetBundleComponent.GetAsset<TextAsset>(ConfigBundle, dataConfig).bytes;
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
