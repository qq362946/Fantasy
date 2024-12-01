namespace Fantasy.Tools.ConfigTable;

public static class ExcelTemplate
{
    public static readonly string Template = """
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
                                             
                                             namespace (namespace)
                                             {
                                                 [ProtoContract]
                                                 public sealed partial class (ConfigName)Data : ASerialize, IConfigTable, IProto
                                                 {
                                                     [ProtoMember(1)]
                                                     public List<(ConfigName)> List { get; set; } = new List<(ConfigName)>();
                                             #if FANTASY_NET
                                                     [ProtoIgnore]
                                                     private readonly ConcurrentDictionary<uint, (ConfigName)> _configs = new ConcurrentDictionary<uint, (ConfigName)>();
                                             #else 
                                                     [ProtoIgnore]
                                                     private readonly Dictionary<uint, (ConfigName)> _configs = new Dictionary<uint, (ConfigName)>();
                                             #endif
                                                     private static (ConfigName)Data _instance = null;
                                             
                                                     public static (ConfigName)Data Instance
                                                     {
                                                         get { return _instance ??= ConfigTableHelper.Load<(ConfigName)Data>(); } 
                                                         private set => _instance = value;
                                                     }
                                             
                                                     public (ConfigName) Get(uint id, bool check = true)
                                                     {
                                                         if (_configs.ContainsKey(id))
                                                         {
                                                             return _configs[id];
                                                         }
                                                 
                                                         if (check)
                                                         {
                                                             throw new Exception($"(ConfigName) not find {id} Id");
                                                         }
                                                         
                                                         return null;
                                                     }
                                                     public bool TryGet(uint id, out (ConfigName) config)
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
                                                 public sealed partial class (ConfigName) : ASerialize, IProto
                                                 {(Fields)  
                                                 } 
                                             }  
                                             """;
}