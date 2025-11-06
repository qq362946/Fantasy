using Fantasy.SourceGenerator.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;
// ReSharper disable MemberHidesStaticFromOuterClass

namespace Fantasy.SourceGenerator.Generators
{
    /// <summary>
    /// SeparateTable 接口生成器
    /// 自动生成 SeparateTable 所需的注册代码，替代运行时反射
    /// </summary>
    [Generator]
    public partial class SeparateTableGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 查找所有标记了 SeparateTableAttribute 的类
            var attributedClasses = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => IsSeparateTableClass(node),
                    transform: static (ctx, _) => GetSeparateTableTypeInfo(ctx))
                .Where(static info => info != null)
                .Collect();
            // 组合编译信息和找到的类型
            var compilationAndTypes = context.CompilationProvider.Combine(attributedClasses);
            // 注册源代码输出
            context.RegisterSourceOutput(compilationAndTypes, static (spc, source) =>
            {
                if (CompilationHelper.IsSourceGeneratorDisabled(source.Left))
                {
                    return;
                }
                
                if (!CompilationHelper.HasFantasyNETDefine(source.Left))
                {
                    return;
                }
                
                if (source.Left.GetTypeByMetadataName("Fantasy.Entitas.Interface.ISupportedSeparateTable") == null)
                {
                    return;
                }

                GenerateRegistrationCode(spc, source.Left, source.Right!);
            });
        }
        
        private static SeparateTableTypeInfo? GetSeparateTableTypeInfo(GeneratorSyntaxContext context)
        {
            var classDecl = (ClassDeclarationSyntax)context.Node;

            if (context.SemanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol symbol)
            {
                return null;
            }
            
            if (!symbol.InheritsFrom("Fantasy.Entitas.Entity"))
            {
                // 不是 Entity，不处理
                return null; 
            }

            // 检查是否标记了 SeparateTableAttribute
            var separateTableAttr = symbol.GetAttributes()
                .Where(attr => attr.AttributeClass?.ToDisplayString() == "Fantasy.SeparateTable.SeparateTableAttribute").ToList();

            if (!separateTableAttr.Any())
            {
                return null;
            }
            
            var separateTableInfo = new Dictionary<string, string>();
                
            foreach (var attributeData in separateTableAttr)
            {
                if (attributeData.ConstructorArguments.Length < 2)
                {
                    return null;
                }

                var rootTypeSymbol = attributeData.ConstructorArguments[0].Value as INamedTypeSymbol;
                var collectionName = attributeData.ConstructorArguments[1].Value?.ToString();

                if (rootTypeSymbol == null || collectionName == null)
                {
                    return null;
                }
                
                separateTableInfo[rootTypeSymbol.GetFullName()] = collectionName;
            }

            return separateTableInfo.Any() ? SeparateTableTypeInfo.Create(symbol, separateTableInfo) : null;
        }

        private static void GenerateRegistrationCode(
            SourceProductionContext context,
            Compilation compilation,
            IEnumerable<SeparateTableTypeInfo> separateTableTypeInfos)
        {
            var separateTableTypeInfoList = separateTableTypeInfos.ToList();
            // 获取当前程序集名称（仅用于注释）
            var assemblyName = compilation.AssemblyName ?? "Unknown";
            // 生成代码文件
            var builder = new SourceCodeBuilder();
            // 添加 using
            builder.AddUsings(
                "System",
                "System.Collections.Generic",
                "Fantasy.Assembly",
                "Fantasy.Entitas",
                "Fantasy.Database",
                "Fantasy.Entitas.Interface",
                "Fantasy.Async"
            );
            builder.AppendLine();
            // 开始命名空间（固定使用 Fantasy.Generated）
            builder.BeginNamespace("Fantasy.Generated");
            // 开始类定义（实现 IEntitySystemRegistrar 接口）
            builder.AddXmlComment($"Auto-generated Entity System registration class for {assemblyName}");
            builder.BeginClass("SeparateTableRegistrar", "internal sealed", "ISeparateTableRegistrar");
            // 生成字段用于存储已注册（用于 UnRegister）
            GenerateFields(builder, separateTableTypeInfoList);
            // 生成注册方法
            GenerateRegisterMethod(builder, separateTableTypeInfoList);
            // 生成反注册方法
            GenerateUnRegisterMethod(builder, separateTableTypeInfoList);
            // 结束类
            builder.EndClass();
            // // 生成数据库帮助类
            // builder.Append(GenerateGenerateSeparateTableGeneratedExtensions().ToString());
            // 结束命名空间
            builder.EndNamespace();
            // 输出源代码
            context.AddSource("SeparateTableRegistrar.g.cs", builder.ToString());
        }

        // private static SourceCodeBuilder GenerateGenerateSeparateTableGeneratedExtensions()
        // {
        //     var builder = new SourceCodeBuilder();
        //     builder.AppendLine();
        //     builder.Indent(1);
        //     builder.AddXmlComment("分表组件扩展方法。");
        //     builder.AppendLine("public static class SeparateTableGeneratedExtensions");
        //     builder.AppendLine("{");
        //     builder.Indent(1);
        //     builder.AddXmlComment("从数据库加载指定实体的所有分表数据，并自动建立父子关系。");
        //     builder.BeginMethod("public static FTask LoadWithSeparateTables<T>(this T entity, IDatabase database) where T : Entity, new()");
        //     builder.AppendLine("return entity.Scene.SeparateTableComponent.LoadWithSeparateTables(entity, database);");
        //     builder.EndMethod();
        //     builder.AddXmlComment("将实体及其所有分表组件保存到数据库中。");
        //     builder.BeginMethod("public static FTask PersistAggregate<T>(this T entity, IDatabase database) where T : Entity, new()");
        //     builder.AppendLine("return entity.Scene.SeparateTableComponent.PersistAggregate(entity,database);");
        //     builder.EndMethod();
        //     builder.Unindent();
        //     builder.AppendLine("}");
        //     return builder;
        // }

        private static void GenerateFields(SourceCodeBuilder builder, List<SeparateTableTypeInfo> separateTableTypeInfoList)
        {
            var separateTableInfoBuilder = new SourceCodeBuilder();
            separateTableInfoBuilder.AppendLine("private readonly List<ISeparateTableRegistrar.SeparateTableInfo> _separateTableInfos = new List<ISeparateTableRegistrar.SeparateTableInfo>()");
            separateTableInfoBuilder.Indent(2);
            separateTableInfoBuilder.AppendLine("{");
            separateTableInfoBuilder.Indent(1);

            try
            {
                if (separateTableTypeInfoList.Any())
                {
                    foreach (var separateTableTypeInfo in separateTableTypeInfoList)
                    {
                        foreach (var separateTableInfo in separateTableTypeInfo.SeparateTableInfo)
                        {
                            separateTableInfoBuilder.AppendLine(
                                $"new ISeparateTableRegistrar.SeparateTableInfo(typeof({separateTableInfo.Key}) ,typeof({separateTableTypeInfo.TypeFullName}) ,\"{separateTableInfo.Value}\"),");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            separateTableInfoBuilder.Unindent();
            separateTableInfoBuilder.AppendLine("};");
            builder.AppendLine(separateTableInfoBuilder.ToString());
        }

        private static void GenerateRegisterMethod(SourceCodeBuilder builder, List<SeparateTableTypeInfo> separateTableTypeInfos)
        {
            builder.BeginMethod("public List<ISeparateTableRegistrar.SeparateTableInfo> Register()");
            builder.AppendLine("return _separateTableInfos;");
            builder.EndMethod();
            builder.AppendLine();
        }

        private static void GenerateUnRegisterMethod(SourceCodeBuilder builder, List<SeparateTableTypeInfo> separateTableTypeInfos)
        {
            builder.BeginMethod("public List<ISeparateTableRegistrar.SeparateTableInfo> UnRegister()");
            builder.AppendLine("return _separateTableInfos;");
            builder.EndMethod();
            builder.AppendLine();
        }
        
        private static bool IsSeparateTableClass(SyntaxNode node)
        {
            if (node is not ClassDeclarationSyntax classDecl)
            {
                return false;
            }

            // 快速检查是否有任何 Attribute
            if (classDecl.AttributeLists.Count == 0)
            {
                return false;
            }

            // 检查是否标记了 SeparateTableAttribute
            // 这里只做简单的语法级别检查，精确的语义检查在 GetSeparateTableTypeInfo 中进行
            foreach (var attributeList in classDecl.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    var attributeName = attribute.Name.ToString();

                    // 匹配以下情况：
                    // 1. [SeparateTable(...)]
                    // 2. [SeparateTableAttribute(...)]
                    // 3. [Fantasy.Entitas.Interface.SeparateTable(...)]
                    // 4. [Fantasy.Entitas.Interface.SeparateTableAttribute(...)]
                    // 5. [global::Fantasy.Entitas.Interface.SeparateTable(...)]
                    if (attributeName == "SeparateTable" ||
                        attributeName == "SeparateTableAttribute" ||
                        attributeName == "Fantasy.Entitas.Interface.SeparateTable" ||
                        attributeName == "Fantasy.Entitas.Interface.SeparateTableAttribute" ||
                        attributeName == "global::Fantasy.Entitas.Interface.SeparateTable" ||
                        attributeName == "global::Fantasy.Entitas.Interface.SeparateTableAttribute")
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private sealed class SeparateTableTypeInfo
        {
            public readonly Dictionary<string, string> SeparateTableInfo;
            public readonly string TypeFullName;
            public readonly string TypeName;

            private SeparateTableTypeInfo(string typeFullName, string typeName,
                Dictionary<string, string> separateTableInfo)
            {
                TypeFullName = typeFullName;
                TypeName = typeName;
                SeparateTableInfo = separateTableInfo;
            }

            public static SeparateTableTypeInfo Create(INamedTypeSymbol symbol,
                Dictionary<string, string> separateTableInfo)
            {
                return new SeparateTableTypeInfo(
                    symbol.GetFullName(),
                    symbol.Name,
                    separateTableInfo);
            }
        }
    }
}
