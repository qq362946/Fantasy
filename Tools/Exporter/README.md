# 安装
1. 安装.net 8 或更高的版本的SDK.
# 配置
导表工具分为客户端和服务器两部分、所以会有很多导出路径需要配置下。
在当前目录中有ExporterSettings.json文件，通过这里可以配置各项的导出路径，具体如下:

- NetworkProtocolTemplatePath ：ProtoBuf生成代码模版的文件位置
- NetworkProtocolDirectory ：ProtoBuf文件所在的文件夹位置
- NetworkProtocolServerDirectory ：ProtoBuf生成到服务端的文件夹位置
- NetworkProtocolClientDirectory ：ProtoBuf生成到客户端的文件夹位置
- ExcelProgramPath ：Excel文件夹的根目录
- ExcelVersionFile ：Excel的Version文件位置、这个文件用于记录每次导出对比是否需要再次导出的文件
- ExcelServerFileDirectory ：Excel生成的代码文件、在服务端文件夹位置
- ExcelClientFileDirectory ：Excel生成的代码文件、在客户端文件夹位置
- ExcelServerBinaryDirectory ：Excel生成服务器二进制数据文件夹位置
- ExcelClientBinaryDirectory ：Excel生成在客户端的二进制数据文件夹位置
- ExcelServerJsonDirectory ：Excel生成在服务端的Json数据文件夹位置
- ExcelClientJsonDirectory ：Excel生成在客户端的Json数据文件夹位置
- ServerCustomExportDirectory ：Excel在服务端生成自定义代码的文件夹位置
- ClientCustomExportDirectory ：Excel在客户端端生成自定义代码的文件夹位置

# 启动导表工具

为了应对不同平台使用、导表工具准备了Run.bat（Windows）、Run.sh（Mac、Linux）两个启动脚本用于在不平平台下使用。

Windows平台下只需要双击Run.bat
Mac、Linux平台下在命令行下执行./Run.sh

# 选择导表操作

启动脚本会提示让你选择导表对应的操作、如下:

1. Client	 // 仅导出客户端相关的操作、一般用于客户端开发人员

2. Server       // 仅导出服务器端的相关的操作、一般用于服务器开发人员

3. All              // 导出客户端和服务器的相关的操作、用于个人开发者或双端开发人员

# 选择需要的导出的类型

选择上述的操作有、您需要再次选择需要导出的类型、比如是导出协议、还是Excel配置表。

1. 导出网络协议 

   // 导出网络协议、不重新生成的协议号、如果有新的协议会再旧的协议下基础上生成、适合已经发布到线上的环境。

2. 导出网络协议并重新生成OpCode

   // 导出网络协议、并重新生成协议号、如果不是线上环境建议使用该选项。

3. 增量导出Excel（包含常量枚举）

   // 只会导出有更改的Excel文件、并包括自定义导出的逻辑

4. 全量导出Excel（包含常量枚举）

   // 会把所有Excel全部重新导出、并包括自定义导出的逻辑
