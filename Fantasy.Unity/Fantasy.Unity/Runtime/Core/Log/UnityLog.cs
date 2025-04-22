using System.Collections.Generic;
#if UNITY_EDITOR
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditorInternal;
#else
using System;
#endif

#if FANTASY_UNITY
namespace Fantasy
{
    public class UnityLog : ILog
    {
        public void Trace(string msg)
        {
            UnityEngine.Debug.Log(msg);
        }

        public void Debug(string msg)
        {
            UnityEngine.Debug.Log(msg);
        }

        public void Info(string msg)
        {
            UnityEngine.Debug.Log(msg);
        }

        public void Warning(string msg)
        {
            UnityEngine.Debug.LogWarning(msg);
        }

        public void Error(string msg)
        {
            UnityEngine.Debug.LogError(msg);
        }

        public void Error(Exception e)
        {
            UnityEngine.Debug.LogException(e);
        }

        public void Trace(string message, params object[] args)
        {
            UnityEngine.Debug.LogFormat(message, args);
        }

        public void Warning(string message, params object[] args)
        {
            UnityEngine.Debug.LogWarningFormat(message, args);
        }

        public void Info(string message, params object[] args)
        {
            UnityEngine.Debug.LogFormat(message, args);
        }

        public void Debug(string message, params object[] args)
        {
            UnityEngine.Debug.LogFormat(message, args);
        }

        public void Error(string message, params object[] args)
        {
            UnityEngine.Debug.LogErrorFormat(message, args);
        }
    }
}
#endif

#if UNITY_EDITOR
namespace Fantasy
{
    /// <summary>
    /// 日志重定向相关的实用函数。
    /// </summary>
    internal static class LogRedirection
    {
        [OnOpenAsset(0)]
        private static bool OnOpenAsset(int instanceID, int line)
        {
            if (line <= 0)
            {
                return false;
            }

            // 获取资源路径
            string assetPath = AssetDatabase.GetAssetPath(instanceID);

            // 判断资源类型
            if (!assetPath.EndsWith(".cs"))
            {
                return false;
            }

            bool autoFirstMatch = assetPath.Contains("Log.cs") ||
                                   assetPath.Contains("UnityLog.cs");

            var stackTrace = GetStackTrace();
            if (!string.IsNullOrEmpty(stackTrace))

            {
                if (!autoFirstMatch)
                {
                    var fullPath = UnityEngine.Application.dataPath.Substring(0, UnityEngine.Application.dataPath.LastIndexOf("Assets", StringComparison.Ordinal));
                    fullPath = $"{fullPath}{assetPath}";
                    // 跳转到目标代码的特定行
                    if (UnityEngine.RuntimePlatform.WindowsEditor == UnityEngine.Application.platform)
                    {
                        fullPath = fullPath.Replace('/', '\\');
                    }
                    InternalEditorUtility.OpenFileAtLineExternal(fullPath, line);
                    return true;
                }
                
                // 先尝试匹配异常堆栈中的确切文件路径（格式为：[0x0000d] in /路径/文件.cs:行号）
                var exceptionMatches = Regex.Matches(stackTrace, @"\[\w+\]\s+in\s+([^\s]+):(\d+)", RegexOptions.Multiline);
                
                // 存储所有匹配的文件和行号
                var validPaths = new List<(string path, int line)>();
                
                // 优先处理异常堆栈中的路径信息
                if (exceptionMatches.Count > 0)
                {
                    for (int i = 0; i < exceptionMatches.Count; i++)
                    {
                        if (exceptionMatches[i].Groups.Count >= 3)
                        {
                            string fullPath = exceptionMatches[i].Groups[1].Value;
                            int fileLine = int.Parse(exceptionMatches[i].Groups[2].Value);
                            
                            // 获取Assets开始的相对路径
                            int assetsIndex = fullPath.IndexOf("Assets");
                            if (assetsIndex >= 0)
                            {
                                string relativePath = fullPath.Substring(assetsIndex);
                                validPaths.Add((relativePath, fileLine));
                            }
                        }
                    }
                }
                else
                {
                    // 如果没有匹配到异常格式，则使用旧的(at ...)格式
                    // 使用正则表达式匹配at的哪个脚本的哪一行
                    var matches = Regex.Match(stackTrace, @"\(at (.+)\)",
                        RegexOptions.IgnoreCase);

                    while (matches.Success)
                    {
                        var pathLine = matches.Groups[1].Value;
    
                        if (!pathLine.Contains("Log.cs") &&
                            !pathLine.Contains("UnityLog.cs"))
                        {
                            var splitIndex = pathLine.LastIndexOf(":", StringComparison.Ordinal);
                            // 脚本路径
                            var path = pathLine.Substring(0, splitIndex);
                            // 行号
                            int fileLine = Convert.ToInt32(pathLine.Substring(splitIndex + 1));
                            
                            // 添加到有效路径列表
                            validPaths.Add((path, fileLine));
                        }
    
                        matches = matches.NextMatch();
                    }
                }
                
                // 如果找到有效的路径
                if (validPaths.Count > 0)
                {
                    // 获取堆栈顶部的文件路径和行号（列表的第一个元素通常是堆栈顶部）
                    string path = validPaths[0].path;
                    int fileLine = validPaths[0].line;
                    
                    var fullPath = UnityEngine.Application.dataPath.Substring(0, UnityEngine.Application.dataPath.LastIndexOf("Assets", StringComparison.Ordinal));
                    fullPath = $"{fullPath}{path}";
                    // 跳转到目标代码的特定行
                    if (UnityEngine.RuntimePlatform.WindowsEditor == UnityEngine.Application.platform)
                    {
                        fullPath = fullPath.Replace('/', '\\');
                    }
                    InternalEditorUtility.OpenFileAtLineExternal(fullPath, fileLine);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取当前日志窗口选中的日志的堆栈信息。
        /// </summary>
        /// <returns>选中日志的堆栈信息实例。</returns>
        private static string GetStackTrace()
        {
            // 通过反射获取ConsoleWindow类
            var consoleWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ConsoleWindow");
            // 获取窗口实例
            var fieldInfo = consoleWindowType.GetField("ms_ConsoleWindow",
                BindingFlags.Static |
                BindingFlags.NonPublic);
            if (fieldInfo != null)
            {
                var consoleInstance = fieldInfo.GetValue(null);
                if (consoleInstance != null)
                    if (EditorWindow.focusedWindow == (EditorWindow)consoleInstance)
                    {
                        // 获取m_ActiveText成员
                        fieldInfo = consoleWindowType.GetField("m_ActiveText",
                            BindingFlags.Instance |
                            BindingFlags.NonPublic);
                        // 获取m_ActiveText的值
                        if (fieldInfo != null)
                        {
                            var activeText = fieldInfo.GetValue(consoleInstance).ToString();
                            return activeText;
                        }
                    }
            }

            return null;
        }
    }
}
#endif
