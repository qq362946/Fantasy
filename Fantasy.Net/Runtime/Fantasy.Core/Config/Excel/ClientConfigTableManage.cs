#if FANTASY_UNITY
using System;
using System.Collections.Generic;

using UnityEngine;

namespace Fantasy
{
    /// <summary>
    /// 配置表管理器，用于加载和管理配置表数据。
    /// </summary>
    public static class ConfigTableManage
    {
        private static readonly string ConfigBundle = "Config".ToLower(); // 配置表资源包名称
        private static readonly Dictionary<string, AProto> ConfigDic = new (); // 配置表数据字典，用于缓存已加载的配置表数据

        static ConfigTableManage()
        {
            AssetBundleHelper.LoadBundle(ConfigBundle);
        }
        
        /// <summary>
        /// 加载指定类型的配置表数据。
        /// </summary>
        /// <typeparam name="T">配置表数据类型。</typeparam>
        /// <returns>加载的配置表数据。</returns>
        public static T Load<T>() where T : AProto
        {
            var dataConfig = typeof(T).Name;
            
            if (ConfigDic.TryGetValue(dataConfig, out var aProto))
            {
                return (T)aProto;
            }
            
            try
            {
                var bytes = AssetBundleHelper.GetAsset<TextAsset>(ConfigBundle, dataConfig).bytes;
                var data = (AProto) ProtoBufHelper.FromBytes(typeof(T), bytes, 0, bytes.Length);
                data.AfterDeserialization();
                ConfigDic[dataConfig] = data;
                return (T)data;
            }
            catch (Exception ex)
            {
                throw new Exception($"ConfigTableManage:{typeof(T).Name} 数据表加载之后反序列化时出错:{ex}");
            }
        }
        
        private static AProto Load(string dataConfig, int assemblyName)
        {
            if (ConfigDic.TryGetValue(dataConfig, out var aProto))
            {
                return aProto;
            }
            
            var fullName = $"Fantasy.{dataConfig}";
            var assembly = AssemblyManager.GetAssembly(assemblyName);
            var type = assembly.GetType(fullName);

            if (type == null)
            {
                Log.Error($"not find {fullName} in assembly");
                return null;
            }
            
            try
            {
                var bytes = AssetBundleHelper.GetAsset<TextAsset>(ConfigBundle, dataConfig).bytes;
                var data = (AProto) ProtoBufHelper.FromBytes(type, bytes, 0, bytes.Length);
                data.AfterDeserialization();
                ConfigDic[dataConfig] = data;
                return data;
            }
            catch (Exception ex)
            {
                throw new Exception($"ConfigTableManage:{type.Name} 数据表加载之后反序列化时出错:{ex}");
            }
        }

        private static void Reload()
        {
            foreach (var (_, aProto) in ConfigDic)
            {
                ((IDisposable) aProto).Dispose();
            }

            ConfigDic.Clear();
        }
    }
}
#endif