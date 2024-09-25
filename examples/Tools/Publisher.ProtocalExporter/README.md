# 安装
1. 安装.net 8 或更高的版本的SDK.
# 配置
协议导出工具配置文件Json。
在当前目录中有ExporterSettings.json文件，通过这里可以配置各项的导出路径，具体如下:

- NetworkProtocolDirectory ：**协议**文件所在的文件夹位置
- NetworkProtocolServerDirectory ：**协议**生成到服务端的文件夹位置
- NetworkProtocolClientDirectory ：**协议**生成到客户端的文件夹位置
- SerializeHelpers ：**协议**序列化辅助器

# SerializesHelper序列化辅助器
**序列化辅助器字段定义所有的序列化 比如(Protobuf MemoryPack MsgPack..)**

- NameSpace：自定义序列化协议所处命名空间
- Attribute：自定义序列化协议类标记的特性标签
- Ignore：自定义序列化工具忽略字段的标记特性标签
- Member：自定义序列化工具标记需要序列化字段的特性标签

        "SerializeHelpers": {
            "ProtoBuf": [
                 {
                     "NameSpace" : "ProtoBuf",
                     "Attribute": "\t[ProtoContract]",
                     "Ignore": "\t\t[ProtoIgnore]",
                     "Member": "ProtoMember"
                 }
            ],
            "MemoryPack": [
                 {
                     "NameSpace" : "MemoryPack",
                     "Attribute": "\t[MemoryPackable]",
                     "Ignore": "\t\t[MemoryPackIgnore]",
                     "Member": "MemoryPackOrder"
                 }
            ],
        }
   

# 启动导出工具

为了应对不同平台使用、导表工具准备了Run.bat（Windows）、Run.sh（Mac、Linux）两个启动脚本用于在不平平台下使用。

Windows平台下只需要双击Run.bat
Mac、Linux平台下在命令行下执行./Run.sh

# 选择导出操作

启动脚本会提示让你选择导表对应的操作、如下:

1. Client	 // 仅导出客户端相关的操作、一般用于客户端开发人员

2. Server       // 仅导出服务器端的相关的操作、一般用于服务器开发人员

3. All              // 导出客户端和服务器的相关的操作、用于个人开发者或双端开发人员

# 选择需要的导出的类型

1. 导出网络协议 

   // 导出网络协议、不重新生成的协议号、如果有新的协议会再旧的协议下基础上生成、适合已经发布到线上的环境。

2. 导出网络协议并重新生成OpCode

   // 导出网络协议、并重新生成协议号、如果不是线上环境建议使用该选项。

