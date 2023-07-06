using Fantasy.Core;

namespace Fantasy;

public static class ExporterHelper
{
    /// <summary>
    /// 配置框架导出配置的路径
    /// </summary>
    public static void Initialize()
    {
        var projectPath = "../../..";
        // ProtoBuf文件所在的位置文件夹位置
        ProtoBufDefine.ProtoBufDirectory = $"{projectPath}/Config/ProtoBuf/";
        // ProtoBuf生成到服务端的文件夹位置
        ProtoBufDefine.ServerDirectory = $"{projectPath}/Server/Fantasy.Hotfix/Generate/NetworkProtocol/";
        // ProtoBuf生成到客户端的文件夹位置
        ProtoBufDefine.ClientDirectory = $"{projectPath}/Client/Unity/Assets/Scripts/Generate/NetworkProtocol/";
        // ProtoBuf生成代码模板的位置
        ProtoBufDefine.ProtoBufTemplatePath = $"{projectPath}/Config/Template/ProtoTemplate.txt";
        // Excel配置文件根目录
        ExcelDefine.ProgramPath = $"{projectPath}/Config/Excel/";
        // Excel版本文件的位置
        ExcelDefine.ExcelVersionFile = $"{ExcelDefine.ProgramPath}Version.txt";
        // Excel生成服务器代码的文件夹位置
        ExcelDefine.ServerFileDirectory = $"{projectPath}/Server/Fantasy.Hotfix/Generate/ConfigTable/Entity/";
        // Excel生成客户端代码文件夹位置
        ExcelDefine.ClientFileDirectory = $"{projectPath}/Client/Unity/Assets/Scripts/Generate/ConfigTable/Entity/";
        // Excel生成服务器二进制数据文件夹位置
        ExcelDefine.ServerBinaryDirectory = $"{projectPath}/Config/Binary/";
        // Excel生成客户端二进制数据文件夹位置
        ExcelDefine.ClientBinaryDirectory = $"{projectPath}/Client/Unity/Assets/Bundles/Config/";
        // Excel生成服务器Json数据文件夹位置
        ExcelDefine.ServerJsonDirectory = $"{projectPath}/Config/Json/Server/";
        // Excel生成客户端Json数据文件夹位置
        ExcelDefine.ClientJsonDirectory = $"{projectPath}/Config/Json/Client/";
        // Excel生成代码模板的位置
        ExcelDefine.ExcelTemplatePath = $"{projectPath}/Config/Template/ExcelTemplate.txt";
        // 服务器自定义导出代码文件夹位置
        ExcelDefine.ServerCustomExportDirectory = $"{projectPath}/Server/Fantasy.Hotfix/Generate/CustomExport/";
        // 客户端自定义导出代码文件夹位置
        ExcelDefine.ClientCustomExportDirectory = $"{projectPath}/Client/Unity/Assets/Scripts/Generate/CustomExport/";
    }
}