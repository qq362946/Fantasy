#if FANTASY_NET
using System;
using ProtoBuf;
using Fantasy;
using System.Linq;
using System.Reflection;
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
    [ProtoContract]
    public sealed partial class WorldConfigData : ASerialize, IConfigTable, IProto
    {
        [ProtoMember(1)]
        public List<WorldConfig> List { get; set; } = new List<WorldConfig>();
#if FANTASY_NET
        [ProtoIgnore]
        private readonly ConcurrentDictionary<uint, WorldConfig> _configs = new ConcurrentDictionary<uint, WorldConfig>();
#else 
        [ProtoIgnore]
        private readonly Dictionary<uint, WorldConfig> _configs = new Dictionary<uint, WorldConfig>();
#endif
        private static WorldConfigData _instance = null;

        public static WorldConfigData Instance
        {
            get { return _instance ??= ConfigTableHelper.Load<WorldConfigData>(); } 
            private set => _instance = value;
        }

        public WorldConfig Get(uint id, bool check = true)
        {
            if (_configs.ContainsKey(id))
            {
                return _configs[id];
            }
    
            if (check)
            {
                throw new Exception($"WorldConfig not find {id} Id");
            }
            
            return null;
        }
        public bool TryGet(uint id, out WorldConfig config)
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
    
    [ProtoContract]
    public sealed partial class WorldConfig : ASerialize, IProto
    {
		[ProtoMember(1)]
		public uint Id { get; set; } // Id
		[ProtoMember(2)]
		public string WorldName { get; set; } // 名称
		[ProtoMember(3)]
		public string DbConnection { get; set; } // 数据库连接字符串
		[ProtoMember(4)]
		public string DbName { get; set; } // 数据库名称
		[ProtoMember(5)]
		public string DbType { get; set; } // 数据库类型  
    } 
}  
#endif