using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using LightProto;
using MongoDB.Bson.Serialization.Attributes;

namespace Fantasy;

public partial class ChatInfo
{
    [BsonIgnore]
    [JsonIgnore]
    [ProtoIgnore]
    [IgnoreDataMember]
    public Scene Scene { get; set; }
}