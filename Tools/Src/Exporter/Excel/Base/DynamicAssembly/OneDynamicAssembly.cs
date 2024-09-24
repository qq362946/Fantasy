using System.Reflection;
using Fantasy.Exporter;
using Fantasy.Serialize;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ProtoBuf;

namespace Exporter.Excel;

public class OneDynamicAssembly
{
    private readonly List<SyntaxTree> _syntaxTreeList = new List<SyntaxTree>();

    public void Load(string file)
    {
        using var fileStream = new StreamReader(file);
        var cSharp = CSharpSyntaxTree.ParseText(fileStream.ReadToEnd());
        _syntaxTreeList.Add(cSharp);
    }
    
    public Assembly Assembly
    {
        get
        {
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
            assemblyMetadata = AssemblyMetadata.CreateFromFile(typeof(ProtoMemberAttribute).Assembly.Location);
            metadataReference = assemblyMetadata.GetReference();
            metadataReferenceList.Add(metadataReference);
            CSharpCompilation compilation = CSharpCompilation.Create(assemblyName, _syntaxTreeList, metadataReferenceList, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

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
    }
}