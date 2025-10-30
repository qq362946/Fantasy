using System.Linq;
using Microsoft.CodeAnalysis;

namespace Fantasy.SourceGenerator.Common
{
    /// <summary>
    /// 编译环境检测帮助类
    /// 提供检测 Fantasy 框架、Unity 环境等公共方法
    /// </summary>
    public static class CompilationHelper
    {
        /// <summary>
        /// 检查是否定义了 Fantasy 框架的预编译符号
        /// 只有定义了 FANTASY_NET的项目才会生成代码
        /// </summary>
        public static bool HasFantasyNETDefine(Compilation compilation)
        {
            // 遍历所有语法树的预处理符号
            foreach (var tree in compilation.SyntaxTrees)
            {
                var defines = tree.Options.PreprocessorSymbolNames;
                if (defines.Contains("FANTASY_NET"))
                {
                    return true;
                }
            }

            return false;
        }
        
        /// <summary>
        /// 检查是否定义了 Fantasy 框架的预编译符号
        /// 只有定义了 FANTASY_UNITY的项目才会生成代码
        /// </summary>
        public static bool HasFantasyUNITYDefine(Compilation compilation)
        {
            // 遍历所有语法树的预处理符号
            foreach (var tree in compilation.SyntaxTrees)
            {
                var defines = tree.Options.PreprocessorSymbolNames;
                if (defines.Contains("FANTASY_UNITY"))
                {
                    return true;
                }
            }

            return false;
        }
        
        /// <summary>
        /// 检查是否定义了 Fantasy 框架的预编译符号
        /// 只有定义了 FANTASY_NET 或 FANTASY_UNITY 的项目才会生成代码
        /// </summary>
        public static bool HasFantasyDefine(Compilation compilation)
        {
            // 遍历所有语法树的预处理符号
            foreach (var tree in compilation.SyntaxTrees)
            {
                var defines = tree.Options.PreprocessorSymbolNames;
                if (defines.Contains("FANTASY_NET") || defines.Contains("FANTASY_UNITY"))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 检测是否是 Unity 编译环境
        /// 优先检查是否引用了 UnityEngine 核心程序集或类型，这是最可靠的方式
        /// </summary>
        public static bool IsUnityCompilation(Compilation compilation)
        {
            // 方法1: 检查是否引用了 UnityEngine.CoreModule 或 UnityEngine 程序集（最可靠）
            foreach (var reference in compilation.References)
            {
                if (reference is PortableExecutableReference peRef)
                {
                    var display = peRef.Display ?? "";
                    // Unity 2017+ 使用模块化程序集
                    if (display.Contains("UnityEngine.CoreModule.dll") ||
                        display.Contains("UnityEngine.dll"))
                    {
                        return true;
                    }
                }
            }

            // 方法2: 检查是否能够找到 UnityEngine 命名空间的核心类型
            var unityMonoBehaviour = compilation.GetTypeByMetadataName("UnityEngine.MonoBehaviour");
            var unityGameObject = compilation.GetTypeByMetadataName("UnityEngine.GameObject");
            if (unityMonoBehaviour != null || unityGameObject != null)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 检查程序集是否显式禁用了 Source Generator
        /// 通过检查是否标注了 [assembly: DisableSourceGenerator] 属性
        /// </summary>
        public static bool IsSourceGeneratorDisabled(Compilation compilation)
        {
            // 获取 DisableSourceGeneratorAttribute 类型
            var disableAttribute = compilation.GetTypeByMetadataName("Fantasy.SourceGenerator.DisableSourceGeneratorAttribute");
            if (disableAttribute == null)
            {
                return false;
            }

            // 检查程序集级别的属性
            foreach (var attribute in compilation.Assembly.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, disableAttribute))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 获取程序集的目标平台类型
        /// 优先级：TargetPlatformAttribute 指定的值 > 预编译符号检测
        /// </summary>
        /// <param name="compilation">编译上下文</param>
        /// <returns>
        /// 0 = Auto/未定义, 1 = Server, 2 = Unity
        /// </returns>
        public static int GetTargetPlatform(Compilation compilation)
        {
            // 1. 首先检查是否有 TargetPlatformAttribute
            var targetPlatformAttribute = compilation.GetTypeByMetadataName("Fantasy.SourceGenerator.TargetPlatformAttribute");

            if (targetPlatformAttribute != null)
            {
                // 检查程序集级别的 TargetPlatformAttribute
                foreach (var attribute in compilation.Assembly.GetAttributes())
                {
                    if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, targetPlatformAttribute))
                    {
                        // 读取构造函数参数（PlatformType 枚举值）
                        if (attribute.ConstructorArguments.Length > 0)
                        {
                            var platformValue = attribute.ConstructorArguments[0].Value;
                            if (platformValue is int platformType)
                            {
                                // 如果不是 Auto (0)，直接返回指定的平台类型
                                if (platformType != 0)
                                {
                                    return platformType;
                                }
                            }
                        }
                        // 如果是 Auto 或没有参数，继续下面的自动检测逻辑
                        break;
                    }
                }
            }

            // 2. 自动检测：根据预编译符号判断
            if (HasFantasyNETDefine(compilation))
            {
                return 1; // Server
            }

            if (HasFantasyUNITYDefine(compilation) && IsUnityCompilation(compilation))
            {
                return 2; // Unity
            }

            // 3. 如果都没有，返回 Auto (0)
            return 0;
        }
    }
}
