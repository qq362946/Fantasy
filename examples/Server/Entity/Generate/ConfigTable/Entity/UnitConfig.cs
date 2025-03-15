using System;
using ProtoBuf;
using Fantasy;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Fantasy.ConfigTable;
using Fantasy.Serialize;
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
    public sealed partial class UnitConfigData : ASerialize, IConfigTable, IProto
    {
        [ProtoMember(1)]
        public List<UnitConfig> List { get; set; } = new List<UnitConfig>();
#if FANTASY_NET
        [ProtoIgnore]
        private readonly ConcurrentDictionary<uint, UnitConfig> _configs = new ConcurrentDictionary<uint, UnitConfig>();
#else 
        [ProtoIgnore]
        private readonly Dictionary<uint, UnitConfig> _configs = new Dictionary<uint, UnitConfig>();
#endif
        private static UnitConfigData _instance = null;

        public static UnitConfigData Instance
        {
            get { return _instance ??= ConfigTableHelper.Load<UnitConfigData>(); } 
            private set => _instance = value;
        }

        public UnitConfig Get(uint id, bool check = true)
        {
            if (_configs.ContainsKey(id))
            {
                return _configs[id];
            }
    
            if (check)
            {
                throw new Exception($"UnitConfig not find {id} Id");
            }
            
            return null;
        }
        public bool TryGet(uint id, out UnitConfig config)
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
    public sealed partial class UnitConfig : ASerialize, IProto
    {
		[ProtoMember(1)]
		public uint Id { get; set; } // Id
		[ProtoMember(2)]
		public string Name { get; set; } // 名称
		[ProtoMember(3)]
		public string Model { get; set; } // 数据库类型
		[ProtoMember(4)]
		public StringDictionaryConfig Dic { get; set; } // 字典类型  
    } 
}  