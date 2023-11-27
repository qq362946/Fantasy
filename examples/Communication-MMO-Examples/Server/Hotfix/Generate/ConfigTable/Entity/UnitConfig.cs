using System;
using ProtoBuf;
using Fantasy;
using System.Linq;
using System.Collections.Generic;
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
#pragma warning disable CS0169
#pragma warning disable CS8618
#pragma warning disable CS8625
#pragma warning disable CS8603

namespace Fantasy
{
    [ProtoContract]
    public sealed partial class UnitConfigData :  AProto, IConfigTable, IDisposable
    {
        [ProtoMember(1)]
        public List<UnitConfig> List { get; set; } = new List<UnitConfig>();
        [ProtoIgnore]
        private readonly Dictionary<uint, UnitConfig> _configs = new Dictionary<uint, UnitConfig>();
        private static UnitConfigData _instance;

        public static UnitConfigData Instance
        {
            get { return _instance ??= ConfigTableManage.Load<UnitConfigData>(); } 
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
            for (var i = 0; i < List.Count; i++)
            {
                UnitConfig config = List[i];
                _configs.Add(config.Id, config);
                config.AfterDeserialization();
            }
    
            base.AfterDeserialization();
        }
        
        public void Dispose()
        {
            Instance = null;
        }
    }
    
    [ProtoContract]
    public sealed partial class UnitConfig : AProto
    {
		[ProtoMember(1, IsRequired  = true)]
		public uint Id { get; set; } // CId
		[ProtoMember(2, IsRequired  = true)]
		public string NickName { get; set; } // 昵称
		[ProtoMember(3, IsRequired  = true)]
		public string ClassName { get; set; } // 职业类型
		[ProtoMember(4, IsRequired  = true)]
		public int MapNum { get; set; } // 默认地图
		[ProtoMember(5, IsRequired  = true)]
		public string Position { get; set; } // 默认位置
		[ProtoMember(6, IsRequired  = true)]
		public string Angle { get; set; } // 默认角度        		     
    } 
}   