using System.Reflection;
using Fantasy;
using Fantasy.Exporter;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Exporter.Excel;

/// <summary>
/// 动态程序集类，用于加载动态生成的程序集并获取动态信息。
/// </summary>
public static class DynamicAssembly
{
    /// <summary>
    /// 加载指定路径下的动态程序集。
    /// </summary>
    /// <param name="path">程序集文件路径。</param>
    /// <returns>加载的动态程序集。</returns>
    public static Assembly Load(string path)
    {
        var fileList = new List<string>();

        // 找到所有需要加载的CS文件

        foreach (string file in Directory.GetFiles(path))
        {
            if (Path.GetExtension(file) != ".cs")
            {
                continue;
            }

            fileList.Add(file);
        }

        var syntaxTreeList = new List<SyntaxTree>();

        foreach (var file in fileList)
        {
            using var fileStream = new StreamReader(file);
            var cSharp = CSharpSyntaxTree.ParseText(fileStream.ReadToEnd());
            syntaxTreeList.Add(cSharp);
        }

        AssemblyMetadata assemblyMetadata;
        MetadataReference metadataReference;
        var currentDomain = AppDomain.CurrentDomain;
        var assemblyName = Path.GetRandomFileName();
        var assemblyArray = currentDomain.GetAssemblies();
        var metadataReferenceList = new List<MetadataReference>();

        // 注册引用

        foreach (var domainAssembly in assemblyArray)
        {
            if (string.IsNullOrEmpty(domainAssembly.Location))
            {
                continue;
            }

            assemblyMetadata = AssemblyMetadata.CreateFromFile(domainAssembly.Location);
            metadataReference = assemblyMetadata.GetReference();
            metadataReferenceList.Add(metadataReference);
        }
        
        // 添加ProtoEntity支持

        assemblyMetadata = AssemblyMetadata.CreateFromFile(typeof(ASerialize).Assembly.Location);
        metadataReference = assemblyMetadata.GetReference();
        metadataReferenceList.Add(metadataReference);

        // 添加MessagePack支持
        
        assemblyMetadata = AssemblyMetadata.CreateFromFile(typeof(MessagePack.MessagePackObjectAttribute).Assembly.Location);
        metadataReference = assemblyMetadata.GetReference();
        metadataReferenceList.Add(metadataReference);

        CSharpCompilation compilation = CSharpCompilation.Create(assemblyName, syntaxTreeList, metadataReferenceList, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();

        var result = compilation.Emit(ms);
        if (!result.Success)
        {
            foreach (var resultDiagnostic in result.Diagnostics)
            {
                Log.Error(resultDiagnostic.GetMessage());
            }

            throw new Exception("failures");
        }

        ms.Seek(0, SeekOrigin.Begin);
        return Assembly.Load(ms.ToArray());
    }

    /// <summary>
    /// 获取动态程序集中指定表格的动态信息。
    /// </summary>
    /// <param name="dynamicAssembly">动态程序集。</param>
    /// <param name="tableName">表格名称。</param>
    /// <returns>动态信息对象。</returns>
    public static DynamicConfigDataType GetDynamicInfo(Assembly dynamicAssembly, string tableName)
    {
        var dynamicConfigDataType = new DynamicConfigDataType
        {
            ConfigDataType = GetConfigType(dynamicAssembly, $"{tableName}Data"),
            ConfigType = GetConfigType(dynamicAssembly, $"{tableName}")
        };

        dynamicConfigDataType.ConfigData = CreateInstance(dynamicConfigDataType.ConfigDataType);
        var listPropertyType = dynamicConfigDataType.ConfigDataType.GetProperty("List");

        if (listPropertyType == null)
        {
            throw new Exception("No Property named Add was found");
        }

        dynamicConfigDataType.Obj = listPropertyType.GetValue(dynamicConfigDataType.ConfigData);
        dynamicConfigDataType.Method = listPropertyType.PropertyType.GetMethod("Add");

        if (dynamicConfigDataType.Method == null)
        {
            throw new Exception("No method named Add was found");
        }

        return dynamicConfigDataType;
    }

    /// <summary>
    /// 根据类型名称获取动态类型。
    /// </summary>
    /// <param name="dynamicAssembly">动态程序集。</param>
    /// <param name="typeName">类型名称。</param>
    /// <returns>动态类型。</returns>
    private static Type GetConfigType(Assembly dynamicAssembly, string typeName)
    {
        var configType = dynamicAssembly.GetType($"Fantasy.{typeName}");
        
        if (configType == null)
        {
            throw new FileNotFoundException($"Fantasy.{typeName} not found");
        }
        
        return configType;
        // return dynamicAssembly.GetType($"Fantasy.{typeName}");
    }

    /// <summary>
    /// 创建动态实例。
    /// </summary>
    /// <param name="configType">动态类型。</param>
    /// <returns>动态实例。</returns>
    public static object CreateInstance(Type configType)
    {
        var config = Activator.CreateInstance(configType);
        
        if (config == null)
        {
            throw new Exception($"{configType.Name} is Activator.CreateInstance error");
        }

        return config;
    }
}