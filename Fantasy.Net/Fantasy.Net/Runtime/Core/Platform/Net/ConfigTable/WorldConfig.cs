#if FANTASY_NET
using System.Collections.Concurrent;
using System.Runtime.Serialization;
using Fantasy.Helper;
using Newtonsoft.Json;
#pragma warning disable CS8601 // Possible null reference assignment.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
namespace Fantasy.Platform.Net
{
    /// <summary>
    /// 存放所有WorldConfigInfo信息
    /// </summary>
    public sealed class WorldConfigData
    {
	    /// <summary>
	    /// 存放所有WorldConfigInfo信息
	    /// </summary>
	    public List<WorldConfig> List;
	    [JsonIgnore] 
	    [IgnoreDataMember]
	    private readonly ConcurrentDictionary<uint, WorldConfig> _configs = new ConcurrentDictionary<uint, WorldConfig>();
	    /// <summary>
	    /// 获得WorldConfig的实例
	    /// </summary>
	    public static WorldConfigData Instance { get; private set; }
	    /// <summary>
	    /// 初始化WorldConfig
	    /// </summary>
	    /// <param name="worldConfigJson"></param>
	    public static void InitializeFromJson(string worldConfigJson)
	    {
		    try
		    {
			    Instance = worldConfigJson.Deserialize<WorldConfigData>();
			    foreach (var config in Instance.List)
			    {
				    Instance._configs.TryAdd(config.Id, config);
			    }
		    }
		    catch (Exception e)
		    {
			    throw new InvalidOperationException($"WorldConfigData.Json format error {e.Message}");
		    }
	    }
	    /// <summary>
	    /// 初始化WorldConfig
	    /// </summary>
	    /// <param name="list"></param>
	    public static void Initialize(List<WorldConfig> list)
	    {
		    Instance = new WorldConfigData
		    {
			    List = list
		    };
		    foreach (var config in Instance.List)
		    {
			    Instance._configs.TryAdd(config.Id, config);
		    }
	    }
	    /// <summary>
	    /// 根据Id获取WorldConfig
	    /// </summary>
	    /// <param name="id"></param>
	    /// <returns></returns>
	    /// <exception cref="FileNotFoundException"></exception>
	    public WorldConfig Get(uint id)
	    {
		    if (_configs.TryGetValue(id, out var worldConfigInfo))
		    {
			    return worldConfigInfo;
		    }
            
		    throw new FileNotFoundException($"WorldConfig not find {id} Id");
	    }
	    /// <summary>
	    /// 根据Id获取WorldConfig
	    /// </summary>
	    /// <param name="id"></param>
	    /// <param name="config"></param>
	    /// <returns></returns>
	    public bool TryGet(uint id, out WorldConfig config)
	    {
		    return _configs.TryGetValue(id, out config);
	    }
    }
    
	/// <summary>
	/// 表示一个世界配置信息
	/// </summary>
	public sealed class WorldConfig
    {
		/// <summary>
		/// Id
		/// </summary>
		public uint Id { get; set; } 
		/// <summary>
		/// 名称
		/// </summary>
		public string WorldName { get; set; }


        #region ↓数据库相关, 在配置多数据库时, 如果未使用推荐的 Fantasy.config 进行配置, 请自行确保索引严格对齐↓

        /// <summary>
        /// 数据库工作职责标识
        /// </summary>
		public int[] DbDuty { get; set; }
        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public string[] DbConnection { get; set; }
        /// <summary>
        /// 数据库名称
        /// </summary>
        public string[] DbName { get; set; }
        /// <summary>
        /// 数据库类型
        /// </summary>
        public string[] DbType { get; set; }

        #endregion
    }
}
#endif