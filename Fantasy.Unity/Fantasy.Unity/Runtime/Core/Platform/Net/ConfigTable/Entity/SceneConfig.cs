#if FANTASY_NET
using System;
using MessagePack;
using Fantasy;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS0169
#pragma warning disable CS8618
#pragma warning disable CS8625
#pragma warning disable CS8603

namespace Fantasy
{
    [MessagePackObject]
    public sealed partial class SceneConfigData : ASerialize, IConfigTable
    {
        [Key(0)]
        public List<SceneConfig> List { get; set; } = new List<SceneConfig>();
#if FANTASY_NET
        [IgnoreMember]
        private readonly ConcurrentDictionary<uint, SceneConfig> _configs = new ConcurrentDictionary<uint, SceneConfig>();
#else 
        [IgnoreMember]
        private readonly Dictionary<uint, SceneConfig> _configs = new Dictionary<uint, SceneConfig>();
#endif
        private static SceneConfigData _instance;

        public static SceneConfigData Instance
        {
            get { return _instance ??= ConfigTableHelper.Load<SceneConfigData>(); } 
            private set => _instance = value;
        }

        public SceneConfig Get(uint id, bool check = true)
        {
            if (_configs.ContainsKey(id))
            {
                return _configs[id];
            }
    
            if (check)
            {
                throw new Exception($"SceneConfig not find {id} Id");
            }
            
            return null;
        }
        public bool TryGet(uint id, out SceneConfig config)
        {
            config = null;
            
            if (!_configs.ContainsKey(id))
            {
                return false;
            }
                
            config = _configs[id];
            return true;
        }
        public override void AfterDeserialization()
        {
            foreach (var config in List)
            {
#if FANTASY_NET
                _configs.TryAdd(config.Id, config);
#else
                _configs.Add(config.Id, config);
#endif
                config.AfterDeserialization();
            }
    
            EndInit();
        }
        
        public override void Dispose()
        {
            Instance = null;
        }
    }
    
    [MessagePackObject]
    public sealed partial class SceneConfig : ASerialize
    {
		[Key(0)]
		public uint Id { get; set; } // ID
		[Key(1)]
		public uint ProcessConfigId { get; set; } // 进程Id
		[Key(2)]
		public uint WorldConfigId { get; set; } // 世界Id
		[Key(3)]
		public string SceneRuntimeType { get; set; } // Scene运行类型
		[Key(4)]
		public string SceneTypeString { get; set; } // Scene类型
		[Key(5)]
		public string NetworkProtocol { get; set; } // 协议类型
		[Key(6)]
		public int OuterPort { get; set; } // 外网端口
		[Key(7)]
		public int InnerPort { get; set; } // 内网端口
		[Key(8)]
		public int SceneType { get; set; } // Scene类型  
    } 
}  
#endif