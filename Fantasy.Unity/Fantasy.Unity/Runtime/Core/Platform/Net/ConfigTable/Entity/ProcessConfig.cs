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
    public sealed partial class ProcessConfigData : ASerialize, IConfigTable
    {
        [Key(0)]
        public List<ProcessConfig> List { get; set; } = new List<ProcessConfig>();
#if FANTASY_NET
        [IgnoreMember]
        private readonly ConcurrentDictionary<uint, ProcessConfig> _configs = new ConcurrentDictionary<uint, ProcessConfig>();
#else 
        [IgnoreMember]
        private readonly Dictionary<uint, ProcessConfig> _configs = new Dictionary<uint, ProcessConfig>();
#endif
        private static ProcessConfigData _instance;

        public static ProcessConfigData Instance
        {
            get { return _instance ??= ConfigTableHelper.Load<ProcessConfigData>(); } 
            private set => _instance = value;
        }

        public ProcessConfig Get(uint id, bool check = true)
        {
            if (_configs.ContainsKey(id))
            {
                return _configs[id];
            }
    
            if (check)
            {
                throw new Exception($"ProcessConfig not find {id} Id");
            }
            
            return null;
        }
        public bool TryGet(uint id, out ProcessConfig config)
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
    public sealed partial class ProcessConfig : ASerialize
    {
		[Key(0)]
		public uint Id { get; set; } // 进程Id
		[Key(1)]
		public uint MachineId { get; set; } // 机器ID
		[Key(2)]
		public bool ReleaseMode { get; set; } // Release下运行  
    } 
}  
#endif