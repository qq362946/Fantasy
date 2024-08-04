#if FANTASY_NET
using ProtoBuf;
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
    public sealed partial class ProcessConfigData :  AProto, IConfigTable, IDisposable
    {
        [ProtoMember(1)]
        public List<ProcessConfig> List { get; set; } = new List<ProcessConfig>();
#if FANTASY_NET
        [ProtoIgnore]
        private readonly ConcurrentDictionary<uint, ProcessConfig> _configs = new ConcurrentDictionary<uint, ProcessConfig>();
#else 
        [ProtoIgnore]
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
    
            base.AfterDeserialization();
        }
        
        public void Dispose()
        {
            Instance = null;
        }
    }
    
    [ProtoContract]
    public sealed partial class ProcessConfig : AProto
    {
		[ProtoMember(1, IsRequired  = true)]
		public uint Id { get; set; } // 进程Id
		[ProtoMember(2, IsRequired  = true)]
		public uint MachineId { get; set; } // 机器ID
		[ProtoMember(3, IsRequired  = true)]
		public bool ReleaseMode { get; set; } // Release下运行        		     
    } 
}  
#endif