# 安装
1. 安装.net 8 或更高的版本的SDK.
# 配置
导表工具分为客户端和服务器两部分、所以会有很多导出路径需要配置下。
在当前目录中有ExporterSettings.json文件，通过这里可以配置各项的导出路径，具体如下:

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


1. 增量导出Excel（包含常量枚举）

   // 只会导出有更改的Excel文件、并包括自定义导出的逻辑

2. 全量导出Excel（包含常量枚举）

   // 会把所有Excel全部重新导出、并包括自定义导出的逻辑
