#if FANTASY_UNITY
using ProtoBuf;
using System.Collections.Generic;

namespace Fantasy
{
    [ProtoContract]
    public sealed class AssetBundleVersion
    {
        [ProtoMember(1)] public string Name;
        [ProtoMember(2)] public string MD5;
        [ProtoMember(3)] public ulong Size;
    }
    
    [ProtoContract]
    public sealed class AssetBundleVersionInfo
    {
        [ProtoMember(1)] public List<AssetBundleVersion> List { get; } = new ();
        [ProtoIgnore] public readonly Dictionary<string, AssetBundleVersion> Dic = new ();
        
        public void Initialize()
        {
            Dic.Clear();
            foreach (var assetBundleVersion in List)
            {
                Dic.Add(assetBundleVersion.Name, assetBundleVersion);
            }
        }
        
        public AssetBundleVersion this[string name]
        {
            get => Dic[name];
            set
            {
                Remove(value.Name);
                Add(value);
            }
        }

        public void Add(AssetBundleVersion assetBundleVersion)
        {
            List.Add(assetBundleVersion);
            Dic.Add(assetBundleVersion.Name, assetBundleVersion);
        }

        public void Remove(string name)
        {
            if (!Dic.Remove(name, out var assetBundleVersion))
            {
                return;
            }
            
            List.Remove(assetBundleVersion);
        }

        public void Remove(AssetBundleVersion assetBundleVersion)
        {
            List.Remove(assetBundleVersion);
            Dic.Remove(assetBundleVersion.Name);
        }
    }
}
#endif