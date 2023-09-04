#if FANTASY_NET
using Fantasy.Core.DataBase;
using Fantasy.Helper;

namespace Fantasy.Core
{
    /// <summary>
    /// 配置表管理器
    /// </summary>
    public static class ConfigTableManage
    {
        /// <summary>
        /// 针对不同类型的配置表提供的委托，用于获取单个服务器配置信息
        /// </summary>
        public static Func<uint, ServerConfigInfo> ServerConfig;
        /// <summary>
        /// 针对不同类型的配置表提供的委托，用于获取单个机器配置信息
        /// </summary>
        public static Func<uint, MachineConfigInfo> MachineConfig;
        /// <summary>
        /// 针对不同类型的配置表提供的委托，用于获取单个场景配置信息
        /// </summary>
        public static Func<uint, SceneConfigInfo> SceneConfig;
        /// <summary>
        /// 针对不同类型的配置表提供的委托，用于获取单个世界配置信息
        /// </summary>
        public static Func<uint, WorldConfigInfo> WorldConfigInfo;
        /// <summary>
        /// 针对不同类型的配置表提供的委托，用于获取全部服务器配置信息列表
        /// </summary>
        public static Func<List<ServerConfigInfo>> AllServerConfig;
        /// <summary>
        /// 针对不同类型的配置表提供的委托，用于获取全部机器配置信息列表
        /// </summary>
        public static Func<List<MachineConfigInfo>> AllMachineConfig;
        /// <summary>
        /// 针对不同类型的配置表提供的委托，用于获取全部场景配置信息列表
        /// </summary>
        public static Func<List<SceneConfigInfo>> AllSceneConfig;
        // 配置表数据缓存字典
        private static readonly Dictionary<string, AProto> ConfigDic = new ();

        /// <summary>
        /// 加载配置表数据
        /// </summary>
        /// <typeparam name="T">配置表类型</typeparam>
        /// <returns>配置表数据</returns>
        public static T Load<T>() where T : AProto
        {
            var dataConfig = typeof(T).Name;
            
            if (ConfigDic.TryGetValue(dataConfig, out var aProto))
            {
                return (T)aProto;
            }
            
            try
            {
                var configFile = GetConfigPath(dataConfig);
                var bytes = File.ReadAllBytes(configFile);
                var data = (T)ProtoBufHelper.FromBytes(typeof(T), bytes, 0, bytes.Length);
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
                var configFile = GetConfigPath(type.Name);
                var bytes = File.ReadAllBytes(configFile);
                var data = (AProto)ProtoBufHelper.FromBytes(type, bytes, 0, bytes.Length);
                data.AfterDeserialization();
                ConfigDic[dataConfig] = data;
                return data;
            }
            catch (Exception ex)
            {
                throw new Exception($"ConfigTableManage:{type.Name} 数据表加载之后反序列化时出错:{ex}");
            }
        }

        /// <summary>
        /// 获取配置表文件路径
        /// </summary>
        /// <param name="name">配置表名称</param>
        /// <returns>配置表文件路径</returns>
        private static string GetConfigPath(string name)
        {
            var configFile = Path.Combine(Define.ExcelServerBinaryDirectory, $"{name}.bytes");

            if (File.Exists(configFile))
            {
                return configFile;
            }

            throw new FileNotFoundException($"{name}.byte not found: {configFile}");
        }

        /// <summary>
        /// 重新加载配置表数据
        /// </summary>
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