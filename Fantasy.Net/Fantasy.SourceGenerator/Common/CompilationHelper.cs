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
    }
}
