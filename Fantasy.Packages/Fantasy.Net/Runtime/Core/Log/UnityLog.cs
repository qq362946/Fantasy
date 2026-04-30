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
        private const string BrandColor = "#60A5FA";
        private const string TraceColor = "#9CA3AF";
        private const string DebugColor = "#38BDF8";
        private const string InfoColor = "#34D399";
        private const string WarningColor = "#FBBF24";
        private const string ErrorColor = "#F87171";
        private const string SceneColor = "#A78BFA";
        private const string MessageColor = "#E5E7EB";

        public void Trace(string msg)
        {
            UnityEngine.Debug.Log(Format("TRACE", TraceColor, msg));
        }

        public void Debug(string msg)
        {
            UnityEngine.Debug.Log(Format("DEBUG", DebugColor, msg));
        }

        public void Info(string msg)
        {
            UnityEngine.Debug.Log(Format("INFO", InfoColor, msg));
        }

        public void Warning(string msg)
        {
            UnityEngine.Debug.LogWarning(Format("WARN", WarningColor, msg));
        }

        public void Error(string msg)
        {
            UnityEngine.Debug.LogError(Format("ERROR", ErrorColor, msg));
        }
        
        public void Trace(string sceneName, string message)
        {
            UnityEngine.Debug.Log(Format("TRACE", TraceColor, sceneName, message));
        }
        
        public void Warning(string sceneName, string message)
        {
            UnityEngine.Debug.LogWarning(Format("WARN", WarningColor, sceneName, message));
        }
        
        public void Info(string sceneName, string message)
        {
            UnityEngine.Debug.Log(Format("INFO", InfoColor, sceneName, message));
        }
        
        public void Debug(string sceneName, string message)
        {
            UnityEngine.Debug.Log(Format("DEBUG", DebugColor, sceneName, message));
        }
        
        public void Error(string sceneName, string message)
        {
            UnityEngine.Debug.LogError(Format("ERROR", ErrorColor, sceneName, message));
        }

        public void Error(Exception e)
        {
            UnityEngine.Debug.LogException(e);
        }

        public void Trace(string message, params object[] args)
        {
            Trace(string.Format(message, args));
        }

        public void Warning(string message, params object[] args)
        {
            Warning(string.Format(message, args));
        }

        public void Info(string message, params object[] args)
        {
            Info(string.Format(message, args));
        }

        public void Debug(string message, params object[] args)
        {
            Debug(string.Format(message, args));
        }

        public void Error(string message, params object[] args)
        {
            Error(string.Format(message, args));
        }
        
        public void Trace(string sceneName, string message, params object[] args)
        {
            Trace(sceneName, string.Format(message, args));
        }
        
        public void Warning(string sceneName, string message, params object[] args)
        {
            Warning(sceneName, string.Format(message, args));
        }
        
        public void Info(string sceneName, string message, params object[] args)
        {
            Info(sceneName, string.Format(message, args));
        }
        
        public void Debug(string sceneName, string message, params object[] args)
        {
            Debug(sceneName, string.Format(message, args));
        }
        
        public void Error(string sceneName, string message, params object[] args)
        {
            Error(sceneName, string.Format(message, args));
        }

        private static string Format(string level, string color, string message)
        {
            return $"<b><color={color}>[{level}]</color></b> <color={MessageColor}>{message}</color>";
        }

        private static string Format(string level, string color, string sceneName, string message)
        {
            return $"<b><color={color}>[{level}]</color></b> <color={SceneColor}>[{sceneName}]</color> <color={MessageColor}>{message}</color>";
        }
    }
}
#endif
