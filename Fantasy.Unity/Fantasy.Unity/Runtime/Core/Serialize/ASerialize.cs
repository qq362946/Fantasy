using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using MessagePack;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Fantasy
{
    public abstract class ASerialize : ISupportInitialize, IDisposable
    {
        public virtual void Dispose() { }
        public virtual void BeginInit() { }
        public virtual void EndInit() { }
        public virtual void AfterDeserialization() => EndInit();
    }

    public abstract class AMessage : ASerialize, IPool
    {
#if FANTASY_NET || FANTASY_UNITY
        [BsonIgnore]
        [JsonIgnore]
        [IgnoreDataMember]
        [IgnoreMember]
        public Scene Scene { get; set; }
#endif
        [BsonIgnore]
        [JsonIgnore]
        [IgnoreDataMember]
        [IgnoreMember]
        public bool IsPool { get; set; }
    }
}