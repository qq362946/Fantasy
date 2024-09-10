# Fantasy.MemoryPack 扩展包
## Unity
Unity使用，只需要再Unity文件夹里，导入Fantays.MemoryPack.unitypackage到Unity里即可。
## Net
 * 在你的项目里用Nuget或dotnet add package MemoryPack安装MemoryPack的库。
 * 再Net文件夹里把MemoryPack文件夹拷贝的项目中。
## Tools
编辑改导表工具ExporterSettings.json文件，在文件Serializes里增加MemoryPack协议的说明.
```json
"Serializes": {
            "Value": [
                {
                     "KeyIndex": 0,
                     "NameSpace" : "MemoryPack",
                     "SerializeName": "MemoryPack",
                     "Attribute": "\t[MemoryPackable]",
                     "Ignore": "\t\t[MemoryPackIgnore]",
                     "Member": "MemoryPackOrder"
                 }
            ],
            "Comment": "自定义序列化器"
        }
```
## 使用注意
无论是Unity或Net下，导入所在的项目，必须要把当前程序集装载到框架中才可以。
